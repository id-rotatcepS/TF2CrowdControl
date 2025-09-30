using ASPEN;

using ConnectorLib.JSON;

using EffectSystem;
using EffectSystem.TF2;

namespace CrowdControl
{
    // started from Celeste CrowdControlHelper example (not much of that is left)

    /// <summary>
    /// the CrowdControlToTF2 Instance establishes a CC connection via SimpleTCPClient,
    /// starts a TF2 version of EffectDispatcher with a CC Effect Responder and feeds it CC EffectRequests 
    /// and exposes all the Effects we claim to support in the game pack and their general status.
    /// </summary>
    public class CrowdControlToTF2
    {
        /// <summary>
        /// the request code sent when a hype train event happens.
        /// request will include a sourceDetails instance of class HypeTrainSourceDetails
        /// </summary>
        public static readonly string CC_HYPETRAIN_CODE = "event-hype-train";

        private static CrowdControlToTF2? _Instance;
        public static CrowdControlToTF2 Instance
            => _Instance
            ??= new CrowdControlToTF2();

        private readonly EffectDispatcher _effectDispatcher;

        private readonly SimpleTCPClient _client;

        public bool CrowdControlConnected => _client.Connected;

        private bool _connected_once = false;

        /// <summary>
        /// Establishes the Crowd Control TCP connection and establishes an EffectDispatcher using it.
        /// The TCP connection starts a 250ms timer to trigger 
        /// <see cref="EffectDispatcher.UpdateUnclosedDurationEffects"/>/<see cref="EffectDispatcher.RefreshEffectListings"/> calls 
        /// and <see cref="OnEffectStatesUpdated"/> events.
        /// TCP requests trigger 
        /// <see cref="EffectDispatcher.Apply(EffectDispatchRequest)"/>/<see cref="EffectDispatcher.StopEarly(EffectDispatchRequest)"/> calls
        /// (and locally processes Tests).
        /// </summary>
        private CrowdControlToTF2()
        {
            _client = new SimpleTCPClient();
            _client.OnConnected += ClientConnected;
            _client.OnRequestReceived += ClientRequestReceived;

            _effectDispatcher = new TF2EffectDispatcher(
                new CCEffectResponder(_client));
        }

        /// <summary>
        /// Exposes <see cref="EffectDispatcher.Effects"/>
        /// </summary>
        public List<Effect> Effects => _effectDispatcher.Effects;

        public EffectDispatcher EffectDispatcher => _effectDispatcher;

        /// <summary>
        /// Disposes (Stops all Effects in) the EffectDispatcher and Disposes local resources.
        /// </summary>
        public void ShutDown()
        {
            _effectDispatcher.Dispose();
            _client.Dispose();
            // not 100% clear whether this is the right timing for this:
            _Instance = null;
        }

        private void ClientConnected()
        {
            _connected_once = true;
            try { _client.OnConnected -= ClientConnected; }
            catch { /**/ }
        }

        private void ClientRequestReceived(SimpleJSONRequest request)
        {
            if (request.IsKeepAlive)
            {
                // type=KeepAlive:
                //  "Can be used in either direction to keep connections open or test connection status.
                //  Responses are neither expected nor given."
                // *  "... if you get a Ping you ideally should Pong, although im not sure the native client actually sends out any of its own.
                // *  Native<->Game communication ... feel free to send a Ping every 30 seconds or whatever"
                //_client.Update(new EffectUpdate() { type = ResponseType.KeepAlive });
                return;
            }
            if (request is EffectRequest effectRequest)
            {
                switch (effectRequest.type)
                {
                    case RequestType.EffectTest:
                        HandleEffectTest(effectRequest);
                        return;
                    case RequestType.EffectStart:
                        HandleEffectStart(effectRequest);
                        return;
                    case RequestType.EffectStop:
                        HandleEffectStop(effectRequest);
                        return;
                    default:
                        // there are no other known types that make an EffectRequest
                        Aspen.Log.Warning($"Unsupported Effect Request Type: {effectRequest.type}");
                        return;
                }
            }
            //not relevant for this game, ignore
            // DataRequest: RequestType.DataRequest
            // RpcResponse: RequestType.RpcResponse
            // PlayerInfo: RequestType.PlayerInfo
            // MessageRequest: RequestType.Login
            // EmptyRequest: RequestType.GameUpdate, RequestType.KeepAlive
        }

        private void HandleEffectStart(EffectRequest request)
        {
            CCEffectDispatchRequest req = CreateCCEffectDispatchRequest(request);

            Aspen.Log.Trace($"Got an effect start request [{request.id}:{request.code}].");

            _effectDispatcher.Apply(req);
        }

        private void HandleEffectStop(EffectRequest request)
        {
            CCEffectDispatchRequest req = CreateCCEffectDispatchRequest(request);

            Aspen.Log.Trace($"Got an effect stop request [{request.id}:{request.code}].");

            _effectDispatcher.StopEarly(req);
        }

        private void HandleEffectTest(EffectRequest request)
        {
            CCEffectDispatchRequest req = CreateCCEffectDispatchRequest(request);

            Aspen.Log.Trace($"Got an effect test request [{request.id}:{request.code}].");

            string effectID = req.EffectID;

            if (effectID == null
                || !_effectDispatcher.Effects.Any(e => e.ID == effectID)
                )
            {
                Aspen.Log.Error($"Effect {effectID} not found. ");// Available effects: {string.Join(", ", Effects.Keys)}");
                //could not find the effect
                _effectDispatcher.Responder.NotAppliedUnavailable(req);
                return;
            }

            Effect effect = _effectDispatcher.Effects.First(e => e.ID == effectID);//Effects[request.code];

            //if (((request.parameters as JArray)?.Count ?? 0) < effect.ParameterTypes.Length)
            //{
            //    //RespondNegative(request, EffectStatus.Failure).Forget();
            //    effectDispatcher.Responder.NotAppliedFailed(req, "wrong parameter count");
            //    return;
            //}

            if (!effect.IsSelectableGameState)
            {
                //Log.Debug($"Effect {effectID} was not ready.");
                _effectDispatcher.Responder.NotAppliedRetry(req);
                return;
            }

            Aspen.Log.Trace($"Effect {effectID} is ready.");
            if (effect.HasDuration)
                _effectDispatcher.Responder.AppliedFor(req, effect.Duration);
            else
                _effectDispatcher.Responder.AppliedInstant(req);
        }

        private static CCEffectDispatchRequest CreateCCEffectDispatchRequest(EffectRequest request)
        {
            if (request.code == CC_HYPETRAIN_CODE
                && request.sourceDetails is HypeTrainSourceDetails hype) // I detest this syntax, but it works so well here.
                return CreateCCHypeTrainEffectDispatchRequest(request, hype);

            return new CCEffectDispatchRequest(request);
        }

        private static CCEffectDispatchRequest CreateCCHypeTrainEffectDispatchRequest(EffectRequest request, HypeTrainSourceDetails hype)
        {
            //hype.Type == req.EffectID;
            string train = $"Level {hype.Level} Hype Train!";
            string progress = $"{hype.Total} bits makes {hype.Progress} towards {hype.Goal}.";
            IEnumerable<string> contributions =
                hype.TopContributions.Select(
                    contrib => $"{contrib.UserName} ({contrib.Total} {contrib.Type})");
            //$"{hype.LastContribution.Total} {hype.LastContribution.Type} from {hype.LastContribution.UserName}";

            Aspen.Log.Info("Hype Train Event: " + train + " " + progress + " " + string.Join(", ", contributions));
            return new CCHypeTrainEffectDispatchRequest(request, train, progress, contributions);
        }

        /// <summary>
        /// Summarizes the effects and what's currently going on with them.
        /// <see cref="EffectDispatcher.GetEffectsStatus"/>
        /// </summary>
        public IEnumerable<EffectState> EffectStates => _effectDispatcher.GetEffectsStatus();
    }
}