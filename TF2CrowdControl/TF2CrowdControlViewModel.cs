using ASPEN;

using CrowdControl;

using EffectSystem;
using EffectSystem.TF2;

using System.ComponentModel;
using System.Reflection;
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
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            this.WindowTitle = string.Format(
                "TF2 Spectator for Crowd Control - {0}.{1}.{2} - by id_rotatcepS",
                version?.Major, version?.Minor, version?.Build);

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

            CCDispatcher = CrowdControlToTF2.Instance;
            InstallConfigsCommand = new InstallConfigsCommand(this);
            CommandLog = string.Empty;

            StartTF2ConnectionAndEffectHandlers();

            CCDispatcher.EffectDispatcher.OnEffectStatesUpdated += (c) =>
            {
                ViewNotification(nameof(StatusEffects));
                ViewNotification(nameof(StatusMapName));
                ViewNotification(nameof(StatusMapNameColor));
                ViewNotification(nameof(StatusClassName));
                ViewNotification(nameof(StatusClassNameColor));
                ViewNotification(nameof(StatusVerticalSpeed));
                ViewNotification(nameof(StatusVerticalSpeedColor));
                ViewNotification(nameof(StatusAppColor));
                ViewNotification(nameof(StatusCCColor));

                ViewNotification(nameof(ProxyValues));
            };
        }

        public string WindowTitle { get; }

        private CrowdControlToTF2 CCDispatcher { get; }

        public Brush StatusCCColor => CCDispatcher.CrowdControlConnected
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.DarkRed);

        public IEnumerable<EffectState> StatusEffects
            => CCDispatcher.EffectStates;

        #region TF2Status
        public Brush StatusAppColor => TF2Effects.Instance.TF2Proxy?.IsOpen ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.DarkRed);

        public string StatusMapName => TF2Effects.Instance.TF2Proxy?.Map ?? string.Empty;
        public Brush StatusMapNameColor => TF2Effects.Instance.TF2Proxy?.IsMapLoaded ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Gray);

        public string StatusClassName => TF2Effects.Instance.TF2Proxy?.ClassSelection ?? string.Empty;
        public Brush StatusClassNameColor => TF2Effects.Instance.TF2Proxy?.IsUserAlive ?? false
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Gray);

        public string StatusVerticalSpeed
        {
            get
            {
                if (TF2Effects.Instance.TF2Proxy == null)
                    return string.Empty;

                double speed = TF2Effects.Instance.TF2Proxy.VerticalSpeed;
                if (double.IsNaN(speed)
                    || speed == 0.0)
                    return string.Empty;

                return speed.ToString();
            }
        }
        public Brush StatusVerticalSpeedColor => TF2Effects.Instance.TF2Proxy?.IsJumping ?? false
            ? new SolidColorBrush(Colors.Red)
            : new SolidColorBrush(Colors.Black);

        public string ProxyValues => (TF2Effects.Instance.TF2Proxy as PollingCacheTF2Proxy)?.AllValues ?? string.Empty;
        #endregion TF2Status

        #region TF2Config
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
        #endregion TF2Config

        public ICommand InstallConfigsCommand { get; }

        public string CommandLog { get; set; }

        //TODO move all the below into a single new class that instantiates TF2Proxy (using TF2Config)
        //& TF2 Effects and loads them into an Effects List (CCDispatcher.Effects => EffectDispatcher.Effects)
        private void StartTF2ConnectionAndEffectHandlers()
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
            try
            {
                TF2Effects.Instance.TF2Proxy?.ShutDown();
            }
            catch (Exception e)
            {
                // This is known to happen when RCON disposes and didn't have an active connection.
                // We don't actually care. We just want to make a new connection.
                Aspen.Log.Trace("Issue while shutting down TF2 connection. " + e.Message);
            }

            try
            {
                TF2Effects.Instance.TF2Proxy = NewTF2Poller();
                // (re)establish Effect instances in the dispatcher via CC instance so they can register variables for proxy to poll.
                //TODO need to stop helper/dispatcher trying when TF2Instance is no good.  have to move control of instance out of the viewmodel, and even then it may not be smart enough to help.
                //TODO however, dispatcher refresh should close things down when the instance is no good.  Mode is bad - hide everything.
                CCDispatcher.Effects.Clear();
                CCDispatcher.Effects.AddRange(TF2Effects.Instance.CreateAllEffects());

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
                Aspen.Log.ErrorException(ex, "Failed Connection to TF2. Change port/password/path settings (or restart this app) to try again.");
            }
        }

        private TF2Proxy NewTF2Poller()
        {
            TF2Instance.WriteRconConfigFile(TF2Config.TF2Path, TF2Config.RCONPort, TF2Config.RCONPassword);
            //FUTURE pass a Microsoft ILogger to RCON to LogError with details when its connection fails
            TF2Instance tf2Instance = TF2Instance.CreateCommunications(TF2Config.RCONPort, TF2Config.RCONPassword);
            // tf2Instance might not be connected, yet, but every SendCommand will attempt to connect again.
            tf2Instance.SetOnDisconnected(RemakeConnection);

            PollingCacheTF2Proxy tf2 = new PollingCacheTF2Proxy(tf2Instance, TF2Config.TF2Path);
            return tf2;
        }

        /// <summary>
        /// ViewModel is getting closed (because the view has been closed).
        /// </summary>
        public void Closed()
        {
            (Aspen.Option as TF2SpectatorSettings).SaveConfig();
            // try to shut down effects, but this doesn't help if TF2 was shut down already.
            this.CCDispatcher.ShutDown();

            TF2Effects.Instance.TF2Proxy?.ShutDown();
        }
    }
}
