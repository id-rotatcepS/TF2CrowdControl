using ASPEN;

using EffectSystem.TF2;

using System.IO;
using System.Windows.Input;

using TF2FrameworkInterface;

namespace TF2CrowdControl
{
    /// <summary>
    /// Alternative to RelayCommand specific to this VM for commands that need its instance.
    /// </summary>
    public abstract class VMCommand : ICommand
    {
        public abstract void Execute(object? arg);
        public abstract bool CanExecute(object? arg);
        internal readonly TF2CrowdControlViewModel vm;

        internal VMCommand(TF2CrowdControlViewModel vm)
        {
            this.vm = vm;
        }

        ///<summary>
        ///Occurs when changes occur that affect whether or not the command should execute.
        ///</summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class InstallConfigsCommand : VMCommand
    {
        internal InstallConfigsCommand(TF2CrowdControlViewModel vm)
            : base(vm)
        { }

        private static TF2Config TF2Config => Aspen.Option.Get<TF2Config>(nameof(TF2Config));

        private string TF2Path => TF2Config.TF2Path;

        private static readonly List<(string file, string content)> configurations = new()
        {
            // required for TF2Instance's RCON
            ("autoexec.cfg",        string.Format("exec {0}", TF2Instance.RconConfigFileBaseName)),

            // required by TF2LogOutput.CustomClassChangeMatcher
            ("scout.cfg",           "echo __class-scout__"),
            ("soldier.cfg",         "echo __class-soldier__"),
            ("pyro.cfg",            "echo __class-pyro__"),
            ("demoman.cfg",         "echo __class-demoman__"),
            ("heavyweapons.cfg",    "echo __class-heavyweapons__"),
            ("engineer.cfg",        "echo __class-engineer__"),
            ("medic.cfg",           "echo __class-medic__"),
            ("sniper.cfg",          "echo __class-sniper__"),
            ("spy.cfg",             "echo __class-spy__"),
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
