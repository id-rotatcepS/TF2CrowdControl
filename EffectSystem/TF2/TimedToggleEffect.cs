namespace EffectSystem.TF2
{
    /// <summary>
    /// always enabled, regardless of current setting
    /// </summary>
    public class TimedToggleEffect : TimedEffect
    {
        public TimedToggleEffect(string id, TimeSpan span, string variable, string value1, string value2)
            : base(id, span)
        {
            Variable = variable;
            Value1 = value1;
            Value2 = value2;
        }
        public TimedToggleEffect(string id, TimeSpan span, string variable)
            : base(id, span)
        {
            Variable = variable;
            Value1 = "0";
            Value2 = "1";
        }

        public string Variable { get; }
        public string Value1 { get; }
        public string Value2 { get; }

        /// <summary>
        /// Always selectable (in a map) because you can always toggle.
        /// </summary>
        public override bool IsSelectableGameState
            => true
            && IsAvailable;

        public override void StartEffect()
        {
            _ = TF2Effects.Instance.RunRequiredCommand(GetToggleCommand());
        }

        private string GetToggleCommand()
        {
            return string.Format("toggle {0} {1} {2}", Variable, Value1, Value2);
        }

        public override void StopEffect()
        {
            _ = TF2Effects.Instance.RunCommand(GetToggleCommand());
        }
    }

    public class NoGunsToggleEffect : TimedToggleEffect
    {
        public static readonly string EFFECT_ID = "no_guns";
        public NoGunsToggleEffect()
            : base(EFFECT_ID, DefaultTimeSpan, "r_drawviewmodel")
        {
            Mutex.Add(TF2Effects.MUTEX_VIEWMODEL);
            // technically works while dead and spectating, but that's not really the point.
            Availability = new AliveInMap();
        }
    }
}