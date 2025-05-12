using ASPEN;

using ConnectorLib.JSON;

using EffectSystem;
using EffectSystem.TF2;

namespace CrowdControl
{
    // started from Celeste example (not much of that is left)

    /// <summary>
    /// the CrowdControlHelper Instance establishes a CC connection via SimpleTCPClient,
    /// starts a CC version of EffectDispatcher and feeds it CC EffectRequests 
    /// plus triggering UpdateUnclosedEffects and RefreshEffectListings on a timer,
    /// and adds all the Effects we claim to support in the game pack
    /// <see cref="CrowdControl.Games.Packs.TF2Spectator.TF2Spectator"/>
    /// </summary>
    public class CrowdControlHelper
    {
        private static CrowdControlHelper? _Instance;
        public static CrowdControlHelper Instance
            => _Instance
            ??= new CrowdControlHelper();

        private readonly EffectDispatcher _effectDispatcher;

        private readonly SimpleTCPClient _client;

        public bool CrowdControlConnected => _client.Connected;

        private bool _connected_once = false;

        private CrowdControlHelper()
        {
            _client = new SimpleTCPClient();
            _client.OnConnected += ClientConnected;
            _client.OnRequestReceived += ClientRequestReceived;

            _effectDispatcher = new EffectDispatcher(
                new CCEffectResponder(_client));

            _effectDispatcher.Effects.AddRange([
                new KillEffect(),
                new ExplodeEffect(),
                new EngineerDestroyBuildingsEffect(),
                new EngineerDestroySentryEffect(),
                new EngineerDestroyDispenserEffect(),
                new EngineerDestroyTeleportersEffect(),
                new SpyRemoveDisguiseEffect(),
                new MedicUberNowEffect(),
                new MedicRadarEffect(),

                new BlackAndWhiteTimedEffect(),
                new SilentMovieTimedEffect(),
                new PixelatedTimedEffect(),
                new DreamTimedEffect(),

                // big and small depending on what they usually use.
                new BigGunsTimedEffect(),
                new SmallGunsTimedEffect(),
                new NoGunsToggleEffect(),
                new LongArmsTimedEffect(),
                new VRModeTimedEffect(),

                new RainbowCrosshairEffect(),
                new CataractsCrosshairEffect(),
                new GiantCrosshairEffect(),
                new BrrrCrosshairEffect(),
                new AlienCrosshairEffect(),

                new MeleeOnlyEffect(),

                new ShowScoreboardEffect(),
                new ShowScoreboardMeanEffect(),
                new MouseSensitivityHighEffect(),
                new MouseSensitivityLowEffect(),
                new QuitEffect(),
                new RetryEffect(),
                new ForcedChangeClassEffect(),
                new SpinEffect(),
                new WM1Effect(),
                new TauntEffect(),
                new TauntContinouslyEffect(),

                new ChallengeMeleeTimedEffect(),
                new SingleTauntAfterKillEffect(),
                new SingleTauntAfterCritKillEffect(),
                new ChallengeCataractsEffect(),
                new ChallengeBlackAndWhiteTimedEffect(),

                new DeathAddsPixelatedTimedEffect(),
                new DeathAddsDreamTimedEffect(),
                ]);
        }

        public void ShutDown()
        {
            _effectDispatcher.StopAll();
            _client.Dispose();
        }

        private void ClientConnected()
        {
            _connected_once = true;
            try { _client.OnConnected -= ClientConnected; }
            catch { /**/ }

            // start the Update timer.
            _timer = new Timer(Tick, null, TickIntervalInMillis, Timeout.Infinite);
            // normally need to dispose timer using await _timer.DisposeAsync(); or _timer.Dispose();
        }

        private Timer? _timer;

        private readonly int TickIntervalInMillis = 250;

        private void Tick(object? state)
        {
            try
            {
                //TODO need to stop trying when TF2Instance is no good.  have to move control of that out of the viewmodel, and even then it may not be smart enough to help.
                //TODO however, refresh should close things down when the instance is no good.  Mode is bad - hide everything.
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
            CCEffectDispatchRequest req = new CCEffectDispatchRequest(request);

            Aspen.Log.Trace($"Got an effect start request [{request.id}:{request.code}].");

            _effectDispatcher.Apply(req);
        }

        private void HandleEffectStop(EffectRequest request)
        {
            CCEffectDispatchRequest req = new CCEffectDispatchRequest(request);

            Aspen.Log.Trace($"Got an effect stop request [{request.id}:{request.code}].");

            _effectDispatcher.StopEarly(req);
        }

        private void HandleEffectTest(EffectRequest request)
        {
            CCEffectDispatchRequest req = new CCEffectDispatchRequest(request);

            Aspen.Log.Trace($"Got an effect test request [{request.id}:{request.code}].");

            if (request.code == null
                || !_effectDispatcher.Effects.Any(e => e.ID == request.code)
                )
            {
                Aspen.Log.Error($"Effect {request.code} not found. ");// Available effects: {string.Join(", ", Effects.Keys)}");
                //could not find the effect
                _effectDispatcher.Responder.NotAppliedUnavailable(req);
                return;
            }
            Effect effect = _effectDispatcher.Effects.First(e => e.ID == request.code);//Effects[request.code];

            //if (((request.parameters as JArray)?.Count ?? 0) < effect.ParameterTypes.Length)
            //{
            //    //RespondNegative(request, EffectStatus.Failure).Forget();
            //    effectDispatcher.Responder.NotAppliedFailed(req, "wrong parameter count");
            //    return;
            //}

            if (!effect.IsSelectableGameState)
            {
                //Log.Debug($"Effect {request.code} was not ready.");
                _effectDispatcher.Responder.NotAppliedRetry(req);
                return;
            }

            Aspen.Log.Trace($"Effect {request.code} is ready.");
            if (effect.HasDuration)
                _effectDispatcher.Responder.AppliedFor(req, effect.Duration);
            else
                _effectDispatcher.Responder.AppliedInstant(req);
        }

        public delegate void EffectStatesUpdated(CrowdControlHelper cc);
        public event EffectStatesUpdated OnEffectStatesUpdated;

        public IEnumerable<EffectState> EffectStates => _effectDispatcher.GetEffectsStatus();
    }
}