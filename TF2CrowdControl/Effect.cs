//using Microsoft.Xna.Framework;

//using Monocle;

namespace CrowdControl
{

    //// adapted from Celeste example
    ///// <summary>
    ///// Game-side Effect that implements the request.
    ///// </summary>
    //public abstract class Effect
    //{
    //    private static int _next_id;
    //    private uint LocalID { get; } = unchecked((uint)Interlocked.Increment(ref _next_id));

    //    // public abstract string Code { get; } // only needed when creating list through reflection.

    //    protected bool _active;
    //    private readonly object _activity_lock = new object();

    //    public TimeSpan Elapsed { get; set; }

    //    public bool IsTimerTicking { get; set; }

    //    //protected Player? Player => CrowdControlHelper.Instance.Player;

    //    public virtual EffectType Type => EffectType.Instant;

    //    public EffectRequest? CurrentRequest { get; private set; }

    //    public TimeSpan Duration { get; private set; } = TimeSpan.Zero;

    //    public virtual TimeSpan DefaultDuration => TimeSpan.Zero;

    //    public virtual Type[] ParameterTypes => System.Type.EmptyTypes;

    //    protected object[] Parameters { get; private set; } = new object[0];

    //    // NOTE only used for bid war I think
    //    public virtual string Group { get; }

    //    public virtual string[] Mutex { get; set; } = new string[0];

    //    private static readonly ConcurrentDictionary<string, bool> _mutexes = new ConcurrentDictionary<string, bool>();

    //    private static bool TryGetMutexes(IEnumerable<string> mutexes)
    //    {
    //        List<string> captured = new List<string>();
    //        bool result = true;
    //        foreach (string mutex in mutexes)
    //        {
    //            if (_mutexes.TryAdd(mutex, true)) { captured.Add(mutex); }
    //            else
    //            {
    //                result = false;
    //                break;
    //            }
    //        }
    //        if (!result) { FreeMutexes(captured); }
    //        return result;
    //    }

    //    public static void FreeMutexes(IEnumerable<string> mutexes)
    //    {
    //        foreach (string mutex in mutexes)
    //        {
    //            _ = _mutexes.TryRemove(mutex, out _);
    //        }
    //    }

    //    public enum EffectType : byte
    //    {
    //        Instant = 0,
    //        Timed = 1,
    //        BidWar = 2
    //    }

    //    public bool Active => _active;

    //    public virtual void Load() => Aspen.Log.Trace($"{GetType().Name} was loaded. [{LocalID}]");

    //    public virtual void Unload() => Aspen.Log.Trace($"{GetType().Name} was unloaded. [{LocalID}]");

    //    public virtual void Start() => Aspen.Log.Trace($"{GetType().Name} was started. [{LocalID}]");

    //    public virtual void End() => Aspen.Log.Trace($"{GetType().Name} was stopped. [{LocalID}]");

    //    //public virtual void Update(GameTime gameTime) => Elapsed += gameTime.ElapsedGameTime;
    //    public virtual void Update() { }// => Elapsed += gameTime.ElapsedGameTime;

    //    //public virtual void Draw(GameTime gameTime) { }

    //    public virtual bool IsReady() => true;//TODO //(Engine.Scene is Level) && (Player.Active);

    //    // //public bool TryStart() => TryStart(new object[0]);

    //    public bool TryStart(EffectRequest request)
    //    {
    //        try
    //        {
    //            lock (_activity_lock)
    //            {
    //                if (Active || (!IsReady())) { return false; }
    //                if (!TryGetMutexes(Mutex)) { return false; }

    //                CurrentRequest = request;

    //                int len = ParameterTypes.Length;
    //                object[] p = new object[len];
    //                for (int i = 0; i < len; i++)
    //                {
    //                    p[i] = Convert.ChangeType(request.parameters[i], ParameterTypes[i]);
    //                }
    //                Parameters = p;

    //                Duration = request.duration.HasValue ? TimeSpan.FromMilliseconds(request.duration.Value) : DefaultDuration;

    //                StartAndSetActive();
    //                return true;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            //NOTE, this method "TryStart" didn't have a try/catch originally,
    //            //so I don't know what the intent was, but this seems clearly the right thing to do.
    //            Aspen.Log.ErrorException(ex, $"Effect {request.code} could not start.");
    //            TryCleanup();
    //            return false;
    //        }
    //    }

    //    private void StartAndSetActive()
    //    {
    //        if (_active == true) { return; }
    //        _active = true;
    //        Elapsed = TimeSpan.Zero;
    //        Start();
    //    }

    //    public bool TryStop()
    //    {
    //        return TryStop(endAction: End);
    //    }
    //    private bool TryStop(Action endAction)
    //    {
    //        try
    //        {
    //            lock (_activity_lock)
    //            {
    //                if (!Active) { return false; }
    //                FreeMutexes(Mutex);
    //                ClearActive();
    //                endAction?.Invoke();
    //                CurrentRequest = null;
    //                return true;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            //NOTE, this method "TryStop" didn't have a try/catch originally,
    //            //so I don't know what the intent was, but this seems clearly the right thing to do.
    //            Aspen.Log.ErrorException(ex, $"Effect could not stop.");
    //            return false;
    //        }
    //    }

    //    private void ClearActive()
    //    {
    //        if (_active == false) { return; }
    //        _active = false;
    //    }

    //    /// <summary>
    //    /// Like TryStop but without calling End()
    //    /// </summary>
    //    private void TryCleanup()
    //    {
    //        _ = TryStop(endAction: () => { });
    //    }
    //}
}