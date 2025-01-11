namespace EffectSystem.TF2
{
    public class TF2Config
    {
        public int ConfigVersion = 0;
        public string TF2Path;
        public ushort RCONPort;
        public string RCONPassword;

        internal const string DefaultTF2Path = @"C:\Program Files (x86)\Steam\steamapps\common\Team Fortress 2";
        public void UpgradeVersionWithDefaults()
        {
            if (ConfigVersion < 1)
            {
                ConfigVersion = 1;
                TF2Path = DefaultTF2Path;
                RCONPort = 48000;
                RCONPassword = "test";
            }

            // additional version defaults start here
        }
    }
}