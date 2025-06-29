using ASPEN;

using ConnectorLib.JSON;

using EffectSystem;

namespace CrowdControl
{
    // started from Celeste example (not much of that is left)

    /// <summary>
    /// the CrowdControlHelper Instance establishes a CC connection via SimpleTCPClient,
    /// starts a CC version of EffectDispatcher and feeds it CC EffectRequests 
    /// plus triggering UpdateUnclosedEffects and RefreshEffectListings on a timer,
    /// and exposes all the Effects we claim to support in the game pack
    /// </summary>
    public class CrowdControlHelper
    {
        /// <summary>
        /// the request code sent when a hype train event happens.
        /// request will include a sourceDetails instance of class HypeTrainSourceDetails
        /// </summary>
        public static readonly string CC_HYPETRAIN_CODE = "event-hype-train";

        private static CrowdControlHelper? _Instance;
        public static CrowdControlHelper Instance
            => _Instance
            ??= new CrowdControlHelper();

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
        private CrowdControlHelper()
        {
            _client = new SimpleTCPClient();
            _client.OnConnected += ClientConnected;
            _client.OnRequestReceived += ClientRequestReceived;

            _effectDispatcher = new EffectDispatcher(
                new CCEffectResponder(_client));
        }

        /// <summary>
        /// Exposes <see cref="EffectDispatcher.Effects"/>
        /// </summary>
        public List<Effect> Effects => _effectDispatcher.Effects;

        /// <summary>
        /// Stops all Effects in the EffectDispatcher and Disposes local resources.
        /// </summary>
        public void ShutDown()
        {
            _effectDispatcher.StopAll();
            _client.Dispose();
            _timer?.Dispose();
            // probably should set _Instance to null.
        }

        private void ClientConnected()
        {
            _connected_once = true;
            try { _client.OnConnected -= ClientConnected; }
            catch { /**/ }

            // start the Update timer.
            _timer = new Timer(Tick, null, TickIntervalInMillis, Timeout.Infinite);
        }

        private Timer? _timer;

        private readonly int TickIntervalInMillis = 250;

        private void Tick(object? state)
        {
            try
            {
                //TODO merge these into one interface call on Dispatcher?
                _effectDispatcher.UpdateUnclosedDurationEffects();
                _effectDispatcher.RefreshEffectListings();
                //TODO make this more granular - dispatcher should invoke when there's actually a change.
                OnEffectStatesUpdated?.Invoke(this);
            }
            finally
            {
                _ = _timer?.Change(TickIntervalInMillis, Timeout.Infinite);
            }
        }

        private void ClientRequestReceived(SimpleJSONRequest request)
        {
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
                        Aspen.Log.Warning($"Unsupported Effect Request Type: {effectRequest.type}");
                        //not relevant for this game, ignore
                        return;
                }
            }
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
            if (request.code != CC_HYPETRAIN_CODE)
                return new CCEffectDispatchRequest(request);

            HypeTrainSourceDetails? hype = request.sourceDetails as HypeTrainSourceDetails;
            if (hype == null)
                return new CCEffectDispatchRequest(request);

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

        public delegate void EffectStatesUpdated(CrowdControlHelper cc);
        public event EffectStatesUpdated? OnEffectStatesUpdated;

        /// <summary>
        /// Summarizes the effects and what's currently going on with them.
        /// <see cref="EffectDispatcher.GetEffectsStatus"/>
        /// </summary>
        public IEnumerable<EffectState> EffectStates => _effectDispatcher.GetEffectsStatus();
    }
}