namespace AttendanceConnect.Utilities
{
    public class SimpleLogger
    {
        private readonly string _logPath;
        private readonly object _lockObj = new();

        public SimpleLogger(string logPath)
        {
            _logPath = logPath;
            EnsureDirectory();
        }

        private void EnsureDirectory()
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>Monday of the week containing the given date.</summary>
        public static DateTime GetWeekStart(DateTime date)
        {
            var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-diff);
        }

        public string GetCurrentLogFilePath()
        {
            return GetLogFilePathForDate(DateTime.Now);
        }

        /// <summary>Log file for the Monday-Sunday week containing the given date (one file per week).</summary>
        public string GetLogFilePathForDate(DateTime date)
        {
            return _logPath.Replace("{Date}", GetWeekStart(date).ToString("yyyy-MM-dd"));
        }

        public void Information(string message)
        {
            Log("INF", message);
        }

        public void Error(string message, Exception? ex = null)
        {
            var msg = ex != null ? $"{message}\n{ex}" : message;
            Log("ERR", msg);
        }

        public void Warning(string message)
        {
            Log("WRN", message);
        }

        private void Log(string level, string message)
        {
            lock (_lockObj)
            {
                try
                {
                    var logFile = GetCurrentLogFilePath();
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var line = $"{timestamp} [{level}] {message}";

                    File.AppendAllText(logFile, line + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}
