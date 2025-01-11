namespace EffectSystem.TF2
{
    public class TF2Effects
    {
        private static TF2Effects _instance;
        public static TF2Effects Instance => _instance ??= new TF2Effects();

        public TF2Proxy? TF2Proxy { get; internal set; }

        public static readonly string MUTEX_VIEWMODEL = "viewmodel";
        public static readonly string MUTEX_WEAPONSLOT = "weaponslot";
        public static readonly string MUTEX_CROSSHAIR_COLOR = "crosshair_color";
        public static readonly string MUTEX_CROSSHAIR_SIZE = "crosshair_size";
        public static readonly string MUTEX_CROSSHAIR_SHAPE = "crosshair_shape";

        public string RunCommand(string command)
        {
            if (TF2Proxy == null)
                //TODO do something.
                return "";
            return TF2Proxy.RunCommand(command);
        }

        public void SetInfo(string variable, string value)
        {
            TF2Proxy?.SetInfo(variable, value);
        }

        public void SetValue(string variable, string value)
        {
            TF2Proxy?.SetValue(variable, value);
        }

        public string GetValue(string variable)
        {
            return TF2Proxy?.GetValue(variable);
        }
    }
}