namespace AttendanceConnect.Models
{
    public class AppSettings
    {
        public DatabaseSettings Database { get; set; } = new();
        public ZKTecoSettings ZKTeco { get; set; } = new();
        public SyncSettings Sync { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class DatabaseSettings
    {
        public string Server { get; set; } = "";
        public string DbName { get; set; } = "";
        public string UserId { get; set; } = "";

        /// <summary>Stored as Base64 in App.config - not encryption, just avoids plain-text display.</summary>
        public string Password { get; set; } = "";
    }

    public class ZKTecoSettings
    {
        public string IP { get; set; } = "";
        public int Port { get; set; }
        public string Key { get; set; } = "";
    }

    public class SyncSettings
    {
        public string DeviceCode { get; set; } = "";
        public int IntervalHours { get; set; }
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
    }

    public class LoggingSettings
    {
        public string LogPath { get; set; } = "";
        public string LogLevel { get; set; } = "";
    }
}
