using AttendanceConnect.Models;
using AttendanceConnect.Utilities;
using System.Net;
using System.Text;

namespace AttendanceConnect.Services
{
    public class ZKTecoService
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly string _key;
        private readonly string _deviceCode;
        private readonly SimpleLogger _logger;

        public ZKTecoService(string ip, int port, string key, string deviceCode, SimpleLogger logger)
        {
            _ip = ip;
            _port = port;
            _key = key;
            _deviceCode = deviceCode;
            _logger = logger;
        }

        public async Task<List<AttendanceLog>> FetchAttendanceLogsAsync()
        {
            var logs = new List<AttendanceLog>();

            try
            {
                _logger.Information($"Fetching attendance logs from ZKTeco device {_ip}:{_port}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // Set up basic auth if needed
                    var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{_key}"));
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                    // Fetch from ZKTeco API - adjust endpoint based on actual ZKTeco device API
                    var url = $"http://{_ip}:{_port}/api/logs";
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        logs = ParseZKTecoLogs(content);
                        _logger.Information($"Successfully fetched {logs.Count} attendance logs from ZKTeco");
                    }
                    else
                    {
                        _logger.Warning($"ZKTeco device returned status code: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error("Failed to connect to ZKTeco device - connection error", ex);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error("ZKTeco device request timeout", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while fetching attendance logs from ZKTeco", ex);
                throw;
            }

            return logs;
        }

        private List<AttendanceLog> ParseZKTecoLogs(string content)
        {
            var logs = new List<AttendanceLog>();

            try
            {
                // Parse based on ZKTeco response format
                // This is a placeholder - adjust based on actual ZKTeco API response format
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Example: Parsing CSV-like format
                    // Format: UserID,VerifyDate,VerifyType,VerifyState,WorkCode
                    var parts = line.Split(',');

                    if (parts.Length >= 2 &&
                        int.TryParse(parts[0], out int userId) &&
                        DateTime.TryParse(parts[1], out DateTime verifyDate))
                    {
                        var log = new AttendanceLog
                        {
                            UserId = userId,
                            VerifyDate = verifyDate,
                            VerifyType = parts.Length > 2 && int.TryParse(parts[2], out int vt) ? vt : null,
                            VerifyState = parts.Length > 3 && int.TryParse(parts[3], out int vs) ? vs : null,
                            WorkCode = parts.Length > 4 && int.TryParse(parts[4], out int wc) ? wc : null,
                            DeviceCode = _deviceCode,
                            ImportedAt = DateTime.Now
                        };

                        logs.Add(log);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error parsing ZKTeco logs", ex);
            }

            return logs;
        }

        public bool TestConnection()
        {
            try
            {
                _logger.Information($"Testing connection to ZKTeco device {_ip}:{_port}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var url = $"http://{_ip}:{_port}/api/health";
                    var response = client.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Information("Successfully connected to ZKTeco device");
                        return true;
                    }
                    else
                    {
                        _logger.Warning($"ZKTeco health check returned: {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to connect to ZKTeco device", ex);
                return false;
            }
        }
    }
}
