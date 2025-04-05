using ASPEN;

using ConnectorLib.JSON;

using Newtonsoft.Json;

using System.Net.Sockets;
using System.Text;

namespace CrowdControl
{

    // adapted from Celeste example

    /// <summary>
    /// Client that connects to Crowd Control at 127.0.0.1:58430.
    /// Exposes a Connected bool.
    /// Exposes Response method (semaphore locked) to send EffectResponses which assumes a connection.
    /// Starts an async loop to establish a connection to make us ready, announced through OnConnected - on an error unreadies and repeats the loop.
    /// Starts an async loop to listen, when ready, for Crowd Control SimpleJSONRequests, exposed through OnRequestReceived.
    /// Starts an async loop to send a "Keep Alive" Response every second if connected.
    /// Cancels all of those on Dispose.
    /// </summary>
    public class SimpleTCPClient : IDisposable
    {
        //TODO just make these (or at least port) constructor args
        private readonly string CROWD_CONTROL_HOST = "127.0.0.1";
        private readonly int APP_CROWD_CONTROL_PORT = 58430;

        private TcpClient _client;
        private readonly SemaphoreSlim _client_lock = new SemaphoreSlim(1);
        private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _error = new ManualResetEventSlim(false);

        private static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public bool Connected { get; private set; }

        private readonly CancellationTokenSource _quitting = new CancellationTokenSource();

        public SimpleTCPClient()
        {
            _ = Task.Factory.StartNew(ConnectLoop, TaskCreationOptions.LongRunning);
            _ = Task.Factory.StartNew(Listen, TaskCreationOptions.LongRunning);
            _ = Task.Factory.StartNew(KeepAlive, TaskCreationOptions.LongRunning);
        }

        ~SimpleTCPClient() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _quitting.Cancel();
            if (disposing)
                _client.Close(); // TODO tiny null pointer risk
        }

        private async void ConnectLoop()
        {
            string errorMessage = string.Empty;
            while (!_quitting.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync();
                    errorMessage = string.Empty;
                }
                catch (Exception e)
                {
                    // Don't log if it's a repeat message.
                    if (errorMessage == e.Message)
                        continue;
                    errorMessage = e.Message;

                    // we're in a loop, so this isn't a serious error, just info.
                    Aspen.Log.InfoException(e, "Crowd Control connect failed - retrying.");
                }
                finally
                {
                    await DisconnectAsync();
                }
            }
            Aspen.Log.Info("Crowd Control connection ended.");
            Connected = false;
        }

        private async Task ConnectAsync()
        {
            _client = new TcpClient()
            {
                ExclusiveAddressUse = false,
                LingerState = new LingerOption(true, 0)
            };
            await _client.ConnectAsync(CROWD_CONTROL_HOST, APP_CROWD_CONTROL_PORT);
            if (!_client.Connected)
                return;

            Connected = true;
            try
            {
                OnConnected?.Invoke();
            }
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, "Crowd Control OnConnected failed");
            }
            _ready.Set();
            _ = await _error.WaitHandle.WaitOneAsync(_quitting.Token);
        }

        private async Task DisconnectAsync()
        {
            Connected = false;
            _error.Reset();
            _ready.Reset();
            try
            {
                _client.Close();
            }
            catch { /**/ }

            if (!_quitting.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(1));
        }

        private async void Listen()
        {
            List<byte> mBytes = new List<byte>();
            byte[] buf = new byte[4096];
            while (!_quitting.IsCancellationRequested)
            {
                try
                {
                    if (!(await _ready.WaitHandle.WaitOneAsync(_quitting.Token)))
                        continue;

                    Socket socket = _client.Client;
                    Aspen.Log.Trace("Listening");

                    int bytesRead = socket.Receive(buf);
                    //Log.Debug($"Got {bytesRead} bytes from socket.");

                    //this is "slow" but the messages are tiny so we don't really care
                    foreach (byte b in buf.Take(bytesRead))
                    {
                        if (b != 0)
                            mBytes.Add(b);
                        else
                            InvokeRequest(mBytes);
                    }
                }
                catch (Exception e)
                {
                    Aspen.Log.ErrorException(e, "Crowd Control Listen failed");
                    _error.Set();
                }
                finally
                {
                    if (!_quitting.IsCancellationRequested)
                        await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        private void InvokeRequest(List<byte> mBytes)
        {
            string json = Encoding.UTF8.GetString(mBytes.ToArray());

            bool parsed = SimpleJSONRequest.TryParse(json, out SimpleJSONRequest? req);
            if (!parsed || req == null)
            {
                Aspen.Log.Error("Crowd Control Request Parse failed");
                return;
            }

            //Log.Debug($"Got a request with ID {req.id}.");
            try { OnRequestReceived?.Invoke(req); }
            catch (Exception e) { Aspen.Log.ErrorException(e, "Crowd Control OnRequestReceived failed"); }
            mBytes.Clear();
        }

        private async void KeepAlive()
        {
            while (!_quitting.IsCancellationRequested)
            {
                try
                {
                    if (Connected)
                        _ = await Respond(new EffectResponse()
                        {
                            id = 0,
                            type = ResponseType.KeepAlive
                        });

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                catch (Exception e)
                {
                    Aspen.Log.ErrorException(e, "Crowd Control KeepAlive failed");
                    _error.Set();
                }
                finally
                {
                    if (!_quitting.IsCancellationRequested)
                        await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        public event Action<SimpleJSONRequest>? OnRequestReceived;
        public event Action? OnConnected;

        /// <summary>
        /// Send an effect response to Crowd Control.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>false if the response failed to be sent for any reason</returns>
        public async Task<bool> Respond(EffectResponse response)
        {
            try
            {
                return await SendCCResponse(response);
            }
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, "Respond to Crowd Control effect response failed");
                return false;
            }
        }

        private async Task<bool> SendCCResponse(object responseObject)
        {
            string json = JsonConvert.SerializeObject(responseObject, JSON_SETTINGS);

            byte[] buffer = Encoding.UTF8.GetBytes(json + '\0');
            Socket socket = _client.Client; // TODO tiny null pointer risk
            await _client_lock.WaitAsync();
            try
            {
                int bytesSent = socket.Send(buffer);
                return bytesSent > 0;
            }
            finally
            {
                _ = _client_lock.Release();
            }
        }

        public async Task<bool> Update(EffectUpdate update)
        {
            try
            {
                return await SendCCResponse(update);
            }
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, "Crowd Control effect Update failed");
                return false;
            }
        }

        //    update.state = GameState.Unknown; // starting state
        //    update.state = GameState.Loading; // no response I guess
        //    //update.state = GameState.Menu;// can't really detect this.
        //    update.state = GameState.Cutscene; // map start movie - maybe map loading.
        //    update.state = GameState.Ready;
        //    update.state = GameState.BadPlayerState; // if I can detect I'm dead?
        public async Task<bool> Update(GameUpdate update)
        {
            try
            {
                return await SendCCResponse(update);
            }
            catch (Exception e)
            {
                Aspen.Log.ErrorException(e, "Crowd Control game Update failed");
                return false;
            }
        }
    }
}