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
            while (!_quitting.IsCancellationRequested)
            {
                try
                {
                    _client = new TcpClient()
                    {
                        ExclusiveAddressUse = false,
                        LingerState = new LingerOption(true, 0)
                    };
                    await _client.ConnectAsync(CROWD_CONTROL_HOST, APP_CROWD_CONTROL_PORT);
                    if (!_client.Connected)
                        continue;

                    Connected = true;
                    try
                    {
                        OnConnected?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Aspen.Log.ErrorException(e, "OnConnected failed");
                    }
                    _ready.Set();
                    _ = await _error.WaitHandle.WaitOneAsync(_quitting.Token);
                }
                catch (Exception e)
                {
                    Aspen.Log.ErrorException(e, "Connect failed");
                }
                finally
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
            }
            Connected = false;
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
                        {
                            //Log.Debug($"Got a complete message: {mBytes.ToArray().ToHexadecimalString()}");
                            string json = Encoding.UTF8.GetString(mBytes.ToArray());
                            //Log.Debug($"Got a complete message: {json}");
                            SimpleJSONRequest req = SimpleJSONRequest.Parse(json);
                            //Log.Debug($"Got a request with ID {req.id}.");
                            try { OnRequestReceived?.Invoke(req); }
                            catch (Exception e) { Aspen.Log.ErrorException(e, "OnRequestReceived failed"); }
                            mBytes.Clear();
                        }
                    }
                }
                catch (Exception e)
                {
                    Aspen.Log.ErrorException(e, "Listen failed");
                    _error.Set();
                }
                finally
                {
                    if (!_quitting.IsCancellationRequested)
                        await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
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
                    Aspen.Log.ErrorException(e, "KeepAlive failed");
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
                Aspen.Log.ErrorException(e, "Respond to effect response failed");
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
                Aspen.Log.ErrorException(e, "Effect update failed");
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
                Aspen.Log.ErrorException(e, "Game update failed");
                return false;
            }
        }
    }
}