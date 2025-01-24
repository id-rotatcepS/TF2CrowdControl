using ASPEN;

using EffectSystem.TF2;

using System.IO;

using TF2FrameworkInterface;

namespace TF2CrowdControl
{
    public class InstallConfigsCommand : VMCommand
    {
        internal InstallConfigsCommand(TF2CrowdControlViewModel vm)
            : base(vm)
        { }

        private static TF2Config TF2Config => Aspen.Option.Get<TF2Config>(nameof(TF2Config));

        private string TF2Path => TF2Config.TF2Path;

        /// <summary>
        /// 0: rcon config file basename
        /// </summary>
        private static string autoexeccfgcontent_format = "exec {0}";
        /// <summary>
        /// sets info_class and echos a custom line
        /// 0: class name
        /// </summary>
        private static string classcfgcontent_format = "setinfo info_class {0};echo __class-{0}__";

        private static readonly List<(string file, string content)> configurations = new()
        {
            // required for TF2Instance's RCON
            ("autoexec.cfg",        string.Format(autoexeccfgcontent_format, TF2Instance.RconConfigFileBaseName)),

            // required by TF2LogOutput.CustomClassChangeMatcher and PollingCacheTF2Proxy
            ("scout.cfg",           string.Format(classcfgcontent_format, "scout")),
            ("soldier.cfg",         string.Format(classcfgcontent_format, "soldier")),
            ("pyro.cfg",            string.Format(classcfgcontent_format, "pyro")),
            ("demoman.cfg",         string.Format(classcfgcontent_format, "demoman")),
            ("heavyweapons.cfg",    string.Format(classcfgcontent_format, "heavyweapons")),
            ("engineer.cfg",        string.Format(classcfgcontent_format, "engineer")),
            ("medic.cfg",           string.Format(classcfgcontent_format, "medic")),
            ("sniper.cfg",          string.Format(classcfgcontent_format, "sniper")),
            ("spy.cfg",             string.Format(classcfgcontent_format, "spy")),
        };

        public override bool CanExecute(object? arg)
            => CanExecute();

        private bool CanExecute()
        {
            // need path, and either no config file, or config file doesn't contain our content
            if (string.IsNullOrWhiteSpace(TF2Path) || !Path.Exists(TF2Path))
                return false;

            foreach ((string file, string content) in configurations)
                if (IsNotConfigured(file, content))
                    return true;

            return false;
        }

        private bool IsNotConfigured(string filename, string filecontent)
        {
            string cfgPath = GetTF2CfgPath(filename);

            if (!File.Exists(cfgPath))
                return true;

            return !File.ReadAllLines(cfgPath).Contains(filecontent);
        }

        private string GetTF2CfgPath(string filename)
        {
            if (IsUsingMastercomfig())
            {
                string MasterComfigUserPath = Path.Combine(TF2Path, @"tf\cfg\user");
                string AutoexecCfgPathMastercomfig = Path.Combine(MasterComfigUserPath, filename);
                return AutoexecCfgPathMastercomfig;
            }

            string AutoexecCfgPath = Path.Combine(TF2Path, @"tf\cfg", filename);
            return AutoexecCfgPath;
        }

        private bool IsUsingMastercomfig()
        {
            if (string.IsNullOrWhiteSpace(TF2Path))
                return false;

            // /tf/custom/mastercomfig*.vpk
            string path = Path.Combine(TF2Path, @"tf\custom");
            return Directory.EnumerateFiles(path).Any(
                n => Path.GetFileName(n).ToLower().StartsWith("mastercomfig")
                && Path.GetExtension(n).ToLower() == ".vpk");
        }

        public override void Execute(object? obj)
            => Execute();

        private void Execute()
        {
            foreach ((string file, string content) in configurations)
                EnsureConfigured(file, content);
        }

        private void EnsureConfigured(string filename, string content)
        {
            if (!IsNotConfigured(filename, content))
                return;

            string cfgPath = GetTF2CfgPath(filename);

            File.AppendAllLines(cfgPath,
                new[] {
                    string.Empty, // in case file ends without a newline.
                    "// Perform configuration for TF2 Spectator:",
                    content
                });
        }
    }
}
