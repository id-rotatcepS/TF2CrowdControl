using ASPEN;

using CrowdControl;

using EffectSystem;
using EffectSystem.TF2;

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

        public string StatusMapName => TF2Effects.Instance.TF2Proxy?.Map ?? string.Empty;
        public Brush StatusMapNameColor => TF2Effects.Instance.TF2Proxy?.IsMapLoaded ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Gray);

        public string StatusClassName => TF2Effects.Instance.TF2Proxy?.ClassSelection ?? string.Empty;
        public Brush StatusClassNameColor => TF2Effects.Instance.TF2Proxy?.IsUserAlive ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Gray);

        public IEnumerable<EffectState> StatusEffects
        {
            get => CC.EffectStates;
        }

        public string ProxyValues => (TF2Effects.Instance.TF2Proxy as PollingCacheTF2Proxy)?.AllValues ?? string.Empty;

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

        public void StartTF2Connection()
        {
            RemakeConnection();

            try
            {
                _ = TF2Effects.Instance.TF2Proxy?.RunCommand("echo test tf2 connection");
            }
            catch (Exception)
            {
                // just a test... if TF2 isn't running that's OK.
                Aspen.Log.Warning("No TF2 connection yet.");
            }
        }

        private void RemakeConnection()
        {
            TF2Effects.Instance.TF2Proxy?.ShutDown();
            try
            {
                TF2Effects.Instance.TF2Proxy = NewTF2Poller();

                TF2Effects.Instance.TF2Proxy.OnUserDied += () => ViewNotification(nameof(StatusClassNameColor));
                TF2Effects.Instance.TF2Proxy.OnUserSpawned += () =>
                {
                    ViewNotification(nameof(StatusClassName));
                    ViewNotification(nameof(StatusClassNameColor));
                    ViewNotification(nameof(StatusMapName));
                };
            }
            catch (Exception ex)
            {
                TF2Effects.Instance.TF2Proxy = null;
                Aspen.Log.ErrorException(ex, "Failed Connection to TF2");
            }
        }

        private TF2Proxy NewTF2Poller()
        {
            TF2Instance tf2Instance = TF2Instance.CreateCommunications(TF2Config.RCONPort, TF2Config.RCONPassword);
            tf2Instance.SetOnDisconnected(RemakeConnection);
            PollingCacheTF2Proxy tf2 = new PollingCacheTF2Proxy(tf2Instance, TF2Config.TF2Path);

            //// subtle indicator of "app thinks you're dead/alive"
            ////TODO delete this or get a better indicator.
            //string DeadCommand =
            //    "cl_hud_playerclass_use_playermodel 0";
            //string AliveCommand =
            //    "cl_hud_playerclass_use_playermodel 1";
            //tf2.OnUserDied += () => _tf2Instance?.SendCommand(new TF2FrameworkInterface.StringCommand(
            //    DeadCommand), (r) => { });
            //tf2.OnUserSpawned += () => _tf2Instance?.SendCommand(new TF2FrameworkInterface.StringCommand(
            //    AliveCommand), (r) => { });

            return tf2;
        }

        /// <summary>
        /// ViewModel is getting closed (because the view has been closed).
        /// </summary>
        public void Closed()
        {
            (Aspen.Option as TF2SpectatorSettings).SaveConfig();
            // try to shut down effects, but this doesn't help if TF2 was shut down already.
            this.CC?.ShutDown();

            TF2Effects.Instance.TF2Proxy?.ShutDown();
        }
    }
}
