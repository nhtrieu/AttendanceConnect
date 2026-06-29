using System.Configuration;
using System.Text;
using AttendanceConnect.Models;

namespace AttendanceConnect.Utilities
{
    /// <summary>Reads/writes settings from App.config (AttendanceConnect.exe.config) appSettings section.</summary>
    public class AppSettingsService
    {
        public AppSettings Settings { get; private set; }

        public AppSettingsService()
        {
            Settings = Load();
        }

        private static AppSettings Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            return new AppSettings
            {
                Database = new DatabaseSettings
                {
                    Server = appSettings["Database.Server"] ?? "",
                    DbName = appSettings["Database.DbName"] ?? "",
                    UserId = appSettings["Database.UserId"] ?? "",
                    Password = appSettings["Database.Password"] ?? ""
                },
                ZKTeco = new ZKTecoSettings
                {
                    IP = appSettings["ZKTeco.IP"] ?? "",
                    Port = int.TryParse(appSettings["ZKTeco.Port"], out var port) ? port : 0,
                    Key = appSettings["ZKTeco.Key"] ?? ""
                },
                Sync = new SyncSettings
                {
                    DeviceCode = appSettings["Sync.DeviceCode"] ?? "",
                    IntervalHours = int.TryParse(appSettings["Sync.IntervalHours"], out var hours) ? hours : 0,
                    StartTime = appSettings["Sync.StartTime"] ?? "",
                    EndTime = appSettings["Sync.EndTime"] ?? ""
                },
                Logging = new LoggingSettings
                {
                    LogPath = appSettings["Logging.LogPath"] ?? "",
                    LogLevel = appSettings["Logging.LogLevel"] ?? ""
                }
            };
        }

        public void Save()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settingsMap = config.AppSettings.Settings;

            SetValue(settingsMap, "Database.Server", Settings.Database.Server);
            SetValue(settingsMap, "Database.DbName", Settings.Database.DbName);
            SetValue(settingsMap, "Database.UserId", Settings.Database.UserId);
            SetValue(settingsMap, "Database.Password", Settings.Database.Password);

            SetValue(settingsMap, "ZKTeco.IP", Settings.ZKTeco.IP);
            SetValue(settingsMap, "ZKTeco.Port", Settings.ZKTeco.Port.ToString());
            SetValue(settingsMap, "ZKTeco.Key", Settings.ZKTeco.Key);

            SetValue(settingsMap, "Sync.DeviceCode", Settings.Sync.DeviceCode);
            SetValue(settingsMap, "Sync.IntervalHours", Settings.Sync.IntervalHours.ToString());
            SetValue(settingsMap, "Sync.StartTime", Settings.Sync.StartTime);
            SetValue(settingsMap, "Sync.EndTime", Settings.Sync.EndTime);

            SetValue(settingsMap, "Logging.LogPath", Settings.Logging.LogPath);
            SetValue(settingsMap, "Logging.LogLevel", Settings.Logging.LogLevel);

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private static void SetValue(KeyValueConfigurationCollection settingsMap, string key, string value)
        {
            if (settingsMap[key] == null)
                settingsMap.Add(key, value);
            else
                settingsMap[key].Value = value;
        }

        public string GetDecodedPassword()
        {
            try
            {
                var bytes = Convert.FromBase64String(Settings.Database.Password);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Not valid Base64 (e.g. plain text from an older config) - use as-is.
                return Settings.Database.Password;
            }
        }

        public void SetPassword(string plainPassword)
        {
            Settings.Database.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainPassword));
        }

        public string GetConnectionString()
        {
            return $"Server={Settings.Database.Server};Database={Settings.Database.DbName};User Id={Settings.Database.UserId};Password={GetDecodedPassword()};";
        }
    }
}
