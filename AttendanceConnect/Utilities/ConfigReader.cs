using System.Text.Json;

namespace AttendanceConnect.Utilities
{
    public class ConfigReader
    {
        private readonly JsonElement _root;

        public ConfigReader(string filePath)
        {
            var json = File.ReadAllText(filePath);
            _root = JsonDocument.Parse(json).RootElement;
        }

        public string GetConnectionString(string key)
        {
            var connStrings = _root.GetProperty("ConnectionStrings");
            return connStrings.GetProperty(key).GetString() ?? "";
        }

        public string GetValue(string section, string key)
        {
            var section_elem = _root.GetProperty(section);
            return section_elem.GetProperty(key).GetString() ?? "";
        }

        public int GetIntValue(string section, string key)
        {
            var section_elem = _root.GetProperty(section);
            return section_elem.GetProperty(key).GetInt32();
        }

        public string GetZKTecoIP()
        {
            var zkTeco = _root.GetProperty("ZKTeco");
            return zkTeco.GetProperty("IP").GetString() ?? "192.168.1.100";
        }

        public int GetZKTecoPort()
        {
            var zkTeco = _root.GetProperty("ZKTeco");
            return zkTeco.GetProperty("Port").GetInt32();
        }

        public string GetZKTecoKey()
        {
            var zkTeco = _root.GetProperty("ZKTeco");
            return zkTeco.GetProperty("Key").GetString() ?? "";
        }

        public string GetDeviceCode()
        {
            var sync = _root.GetProperty("Sync");
            return sync.GetProperty("DeviceCode").GetString() ?? "DEVICE_001";
        }

        public int GetSyncInterval()
        {
            var sync = _root.GetProperty("Sync");
            return sync.GetProperty("IntervalHours").GetInt32();
        }

        public string GetStartTime()
        {
            var sync = _root.GetProperty("Sync");
            return sync.GetProperty("StartTime").GetString() ?? "08:30";
        }

        public string GetEndTime()
        {
            var sync = _root.GetProperty("Sync");
            return sync.GetProperty("EndTime").GetString() ?? "20:00";
        }

        public string GetLogPath()
        {
            var logging = _root.GetProperty("Logging");
            return logging.GetProperty("LogPath").GetString() ?? "./logs/attendance-{Date}.log";
        }
    }
}
