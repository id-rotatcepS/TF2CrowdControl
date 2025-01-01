using ASPEN;

using Effects.TF2;

using Newtonsoft.Json;

using System.IO;
using System.Reflection;

namespace TF2CrowdControl
{
    /// <summary>
    /// Load/Save user settings keyed by the viewmodel's property names.
    /// </summary>
    internal class TF2SpectatorSettings : AspenUserSettings
    {
        /// <summary>
        /// Get the path for this file, 
        /// trying for the ApplicationData (roaming, %appdata%) or else LocalApplicationData folder
        /// in the AssemblyTitle subfolder.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetConfigFilePath(string file)
        {
            string configPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(configPath))
                configPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return GetFilePath(configPath, file);
        }

        private static string GetFilePath(string configPath, string file)
        {
            string title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;

            string folder = Path.Combine(configPath, title);

            if (!Directory.Exists(folder))
                _ = Directory.CreateDirectory(folder);

            return Path.Combine(folder, file);
        }

        /// <summary>
        /// Always in the Local (non-roaming) path. otherwise the same as <see cref="GetConfigFilePath(string)"/>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetBackupFilePath(string file)
        {
            string configPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return GetFilePath(configPath, file);
        }

        //public readonly static string ConfigFilename = "TF2Spectator.config.txt";
        //public readonly static string ConfigFilePath = GetConfigFilePath(ConfigFilename);

        public readonly static string TF2ConfigFilename = "TF2Config.json";
        public readonly static string TF2ConfigFilePath = GetConfigFilePath(TF2ConfigFilename);

        public TF2SpectatorSettings()
        {
            LoadConfig();
        }

        public TF2Config TF2 => Aspen.Option.Get<TF2Config>(nameof(TF2Config));

        Dictionary<object, object> options = new Dictionary<object, object>();
        public T Get<T>(object key)
        {
            object value = options[key];
            if (value is T t)
                return t;
            throw new InvalidCastException();
        }

        public void Set<T>(object key, T value)
        {
            options[key] = value;
        }

        private void LoadConfig()
        {
            LoadTF2Config();

            //LoadTwitchConfig();
            //options[nameof(TF2WindowsViewModel.TwitchUsername)] = lines.Length > 0 ? lines[0] : DefaultUserName;
            //options[nameof(TF2WindowsViewModel.AuthToken)] = lines.Length > 1 ? lines[1] : string.Empty;
            //options[nameof(TF2WindowsViewModel.TwitchConnectMessage)] = lines.Length > 6 ? lines[6] : DefaultConnectMessage;

            //LoadCCConfig();
        }

        private void LoadTF2Config()
        {
            //options[nameof(TF2WindowsViewModel.BotDetectorLog)] = lines.Length > 5 ? lines[5] : string.Empty;
            //options[nameof(TF2WindowsViewModel.SteamUUID)] = lines.Length > 7 ? lines[7] : "[U:1:123456]";

            TF2Config? config = null;
            try
            {
                string json = File.ReadAllText(TF2ConfigFilePath);
                config = JsonConvert.DeserializeObject<TF2Config>(json);
            }
            catch (FileNotFoundException)
            {
                // expected
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, "Loading TF2Config");
            }

            // default
            config ??= new TF2Config();
            config.UpgradeVersionWithDefaults();

            options[nameof(TF2Config)] = config;
        }


        internal void SaveConfig()
        {
            SaveTF2Config();
            //SaveTwitchConfig();
            ////content.AppendLine(ASPEN.Aspen.Option.Get<string>(nameof(TF2WindowsViewModel.TwitchUsername)));
            ////content.AppendLine(ASPEN.Aspen.Option.Get<string>(nameof(TF2WindowsViewModel.AuthToken)));

            ////content.AppendLine(ASPEN.Aspen.Option.Get<string>(nameof(TF2WindowsViewModel.TwitchConnectMessage)));
        }

        private void SaveTF2Config()
        {
            ////content.AppendLine(ASPEN.Aspen.Option.Get<string>(nameof(TF2WindowsViewModel.BotDetectorLog)));
            ////content.AppendLine(ASPEN.Aspen.Option.Get<string>(nameof(TF2WindowsViewModel.SteamUUID)));
            try
            {
                string json = JsonConvert.SerializeObject(Aspen.Option.Get<TF2Config>(nameof(TF2Config)));
                File.WriteAllText(TF2ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Aspen.Log.ErrorException(ex, "Saving TF2Config");
            }
        }
    }
}
