namespace EffectSystem.TF2
{
    public class TF2Effects
    {
        private static TF2Effects? _instance;
        public static TF2Effects Instance
            => _instance
            ??= new TF2Effects();

        public TF2Proxy? TF2Proxy { get; internal set; }

        public static readonly string MUTEX_VIEWMODEL = "viewmodel";
        public static readonly string MUTEX_FOV = "fov";
        public static readonly string MUTEX_WEAPONSLOT = "weaponslot";
        public static readonly string MUTEX_CROSSHAIR_COLOR = "crosshair_color";
        public static readonly string MUTEX_CROSSHAIR_SIZE = "crosshair_size";
        public static readonly string MUTEX_CROSSHAIR_SHAPE = "crosshair_shape";
        public static readonly string MUTEX_SCOREBOARD = "scoreboard";
        public static readonly string MUTEX_MOUSE = "mouse";
        public static readonly string MUTEX_FORCE_MOVE_ROTATE = "force_move_rotate";
        public static readonly string MUTEX_FORCE_MOVE_STRAFE = "force_move_strafe";
        public static readonly string MUTEX_FORCE_MOVE_ATTACK = "force_move_attack";
        public static readonly string MUTEX_FORCE_MOVE_FORWARD = "force_move_forward";
        public static readonly string MUTEX_FORCE_MOVE_JUMP = "force_move_jump";
        public static readonly string MUTEX_VIEWPORT = "viewport";
        public static readonly string MUTEX_BLOOM = "bloom";

        public string RunCommand(string command)
        {
            try
            {
                return RunRequiredCommand(command);
            }
            catch (Exception ex)
            {
                ASPEN.Aspen.Log.Warning(ex.Message);
                return "";
            }
        }

        public string RunRequiredCommand(string command)
        {
            if (TF2Proxy == null)
                throw new EffectNotAppliedException(string.Format("No connection available to run command: {0}", command));

            return TF2Proxy.RunCommand(command);
        }

        public void SetInfo(string variable, string value)
        {
            if (TF2Proxy == null)
            {
                ASPEN.Aspen.Log.Warning(string.Format("No connection available to set info: {0} = {1}", variable, value));
                return;
            }

            TF2Proxy?.SetInfo(variable, value);
        }

        public void SetValue(string variable, string value)
        {
            try
            {
                SetRequiredValue(variable, value);
            }
            catch (Exception ex)
            {
                ASPEN.Aspen.Log.Warning(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <exception cref="EffectNotAppliedException"></exception>
        public void SetRequiredValue(string variable, string value)
        {
            if (TF2Proxy == null)
                throw new EffectNotAppliedException(string.Format("No connection available to set value: {0} = {1}", variable, value));

            TF2Proxy?.SetValue(variable, value);
        }

        public string? GetValue(string variable)
        {
            return TF2Proxy?.GetValue(variable);
        }
    }
}