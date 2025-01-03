using ASPEN;

using CrowdControl;

using Effects;
using Effects.TF2;

using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

using TF2FrameworkInterface;

namespace TF2CrowdControl
{
    /// <summary>
    /// just adds to viewmodel's "CommandLog" property
    /// </summary>
    internal class TF2SpectatorLog : ASPEN.AspenLogging
    {
        private TF2CrowdControlViewModel vm;

        public TF2SpectatorLog(TF2CrowdControlViewModel viewModel)
        {
            this.vm = viewModel;
        }

        private void AddLog(string msg)
        {
            vm.CommandLog = vm.CommandLog + "\n" + msg;
            vm.ViewNotification(nameof(vm.CommandLog));
        }

        public void Error(string msg)
        {
            AddLog(msg);
        }

        public void ErrorException(Exception exc, string msg)
        {
            AddLog(msg + "\n - " + GetMessages(exc));
        }

        private string GetMessages(Exception e)
        {
            string messages = e.Message;
            while (e.InnerException != null)
            {
                e = e.InnerException;
                messages += ";" + e.Message;
            }
            return messages;
        }

        public void Info(string msg)
        {
            AddLog(msg);
        }

        public void InfoException(Exception exc, string msg)
        {
            AddLog(msg + "\n - " + GetMessages(exc));
        }

        public void Trace(string msg)
        //#if DEBUG
        //         {   Info(msg); }
        //#else
        { }
        //#endif

        public void Warning(string msg)
        {
            AddLog(msg);
        }

        public void WarningException(Exception exc, string msg)
        {
            AddLog(msg + "\n - " + GetMessages(exc));
        }
    }

    internal class TF2CrowdControlViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        public void ViewNotification(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotifyPropertyChanged


        public TF2CrowdControlViewModel()
        {
            Aspen.Log = new TF2SpectatorLog(this);
            // settings load/save to the config file.
            Aspen.Option = new TF2SpectatorSettings();

            //Aspen.Log = new DefaultLogUtility();
            //(Aspen.Log as DefaultLogUtility).LogOutput = Console.Out;
            //Aspen.Track = new DefaultCommandContextUtility();
            //Aspen.Track.Start(Aspen.Track.CreateContextFor(nameof(TF2CrowdControlViewModel)));
            //Aspen.Option = new DefaultUserSettingUtility();
            //Aspen.Config = Aspen.Config;
            //Aspen.Text
            //Aspen.Show

            CC = CrowdControlHelper.Instance;
            InstallConfigsCommand = new InstallConfigsCommand(this);
            CommandLog = string.Empty;

            StartTF2Connection();

            CC.OnEffectStatesUpdated += (c) =>
            {
                ViewNotification(nameof(StatusEffects));
                ViewNotification(nameof(StatusMapName));
                ViewNotification(nameof(StatusMapNameColor));
                ViewNotification(nameof(StatusClassName));
                ViewNotification(nameof(StatusClassNameColor));

                ViewNotification(nameof(ProxyValues));
            };
        }

        public CrowdControlHelper CC { get; }

        public string StatusMapName => TF2Effects.TF2Proxy?.Map ?? string.Empty;
        public Brush StatusMapNameColor => TF2Effects.TF2Proxy?.IsMapLoaded ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Gray);

        public string StatusClassName => TF2Effects.TF2Proxy?.ClassSelection ?? string.Empty;
        public Brush StatusClassNameColor => TF2Effects.TF2Proxy?.IsUserAlive ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Gray);

        public IEnumerable<EffectState> StatusEffects
        {
            get => CC.EffectStates;
        }

        public string ProxyValues => (TF2Effects.TF2Proxy as TF2Poller)?.AllValues ?? string.Empty;

        private static TF2Config TF2Config => Aspen.Option.Get<TF2Config>(nameof(TF2Config));

        /// <summary>
        /// ...\steamapps\common\Team Fortress 2
        /// </summary>
        public string TF2Path
        {
            get => TF2Config.TF2Path;
            set
            {
                TF2Config.TF2Path = value?.Trim();
                // no impact on rcon instance (no _tf2 = null;)
                ViewNotification(nameof(TF2Path));
                RemakeConnection();
            }
        }

        public string RCONPassword
        {
            get => TF2Config.RCONPassword;
            set
            {
                TF2Config.RCONPassword = value?.Trim();
                // no impact on rcon instance (no _tf2 = null;)
                ViewNotification(nameof(RCONPassword));
                RemakeConnection();
            }
        }

        public ushort RCONPort
        {
            get => TF2Config.RCONPort;
            set
            {
                TF2Config.RCONPort = value;
                // no impact on rcon instance (no _tf2 = null;)
                ViewNotification(nameof(RCONPort));
                RemakeConnection();
            }
        }

        public ICommand InstallConfigsCommand { get; }

        public string CommandLog { get; set; }

        private TF2Instance? _tf2
        {
            get => TF2Effects.TF2Instance;
            set => TF2Effects.TF2Instance = value;
        }

        public void StartTF2Connection()
        {
            RemakeConnection();

            Task respondedTask = _tf2.SendCommand(
                new StringCommand("echo hi"),
                (s) => { });
            //respondedTask.Wait();

            //CrowdControlHelper.Instance.Active;
            //CrowdControlHelper.Instance.Update();
        }

        private void RemakeConnection()
        {
            _tf2 = TF2Instance.CreateCommunications(RCONPort, RCONPassword);
            _tf2.SetOnDisconnected(RemakeConnection);
            TF2Effects.TF2Proxy.OnUserDied += () => ViewNotification(nameof(StatusClassNameColor));
            TF2Effects.TF2Proxy.OnUserSpawned += () =>
            {
                ViewNotification(nameof(StatusClassName));
                ViewNotification(nameof(StatusClassNameColor));
                ViewNotification(nameof(StatusMapName));
            };

            //_tf2.SetOnDisconnected(() => _tf2 = null);

            //_tf2.ShouldProcessResultValues = true;//default
            //_tf2.TF2RCON
        }

        /// <summary>
        /// ViewModel is getting closed (because the view has been closed).
        /// </summary>
        public void Closed()
        {
            (Aspen.Option as TF2SpectatorSettings).SaveConfig();
            // try to shut down effects, but this doesn't help if TF2 was shut down already.
            this.CC?.ShutDown();
        }
    }
}
