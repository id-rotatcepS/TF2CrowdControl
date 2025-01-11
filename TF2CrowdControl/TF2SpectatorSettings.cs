using ASPEN;

using EffectSystem.TF2;

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
        }

        private void LoadTF2Config()
        {
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
        }

        private void SaveTF2Config()
        {
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
