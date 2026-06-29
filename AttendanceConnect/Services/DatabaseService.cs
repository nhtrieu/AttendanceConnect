using AttendanceConnect.Models;
using AttendanceConnect.Utilities;
using System.Data.SqlClient;

namespace AttendanceConnect.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly SimpleLogger _logger;

        public DatabaseService(string connectionString, SimpleLogger logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<int> InsertAttendanceLogsAsync(List<AttendanceLog> logs)
        {
            if (logs == null || logs.Count == 0)
            {
                _logger.Warning("No attendance logs to insert");
                return 0;
            }

            int insertedCount = 0;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var log in logs)
                    {
                        try
                        {
                            if (await InsertLogAsync(connection, log))
                            {
                                insertedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"Failed to insert log for UserID {log.UserId} at {log.VerifyDate}: {ex.Message}");
                        }
                    }

                    connection.Close();
                }
            }
            catch (SqlException ex)
            {
                _logger.Error("Database connection error while inserting logs", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while inserting attendance logs", ex);
                throw;
            }

            return insertedCount;
        }

        private async Task<bool> InsertLogAsync(SqlConnection connection, AttendanceLog log)
        {
            const string query = @"
                INSERT INTO [dbo].[tblHR_AttendanceLogs]
                (UserID, VerifyDate, VerifyType, VerifyState, WorkCode, DeviceCode, ImportedAt)
                VALUES
                (@UserId, @VerifyDate, @VerifyType, @VerifyState, @WorkCode, @DeviceCode, @ImportedAt)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", log.UserId);
                command.Parameters.AddWithValue("@VerifyDate", log.VerifyDate);
                command.Parameters.AddWithValue("@VerifyType", log.VerifyType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@VerifyState", log.VerifyState ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@WorkCode", log.WorkCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DeviceCode", log.DeviceCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ImportedAt", log.ImportedAt);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }

        public async Task<DateTime?> GetLastVerifyDateAsync()
        {
            const string query = "SELECT TOP 1 VerifyDate FROM [dbo].[tblHR_AttendanceLogs] ORDER BY VerifyDate DESC";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return result is DateTime verifyDate ? verifyDate : null;
            }
            catch (SqlException ex)
            {
                _logger.Error("Database connection error while reading last VerifyDate", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while reading last VerifyDate", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetStaffNamesAsync()
        {
            const string query = "SELECT AttendanceUserID, FullName FROM vwAL_StaffAttendance";
            var staffNames = new Dictionary<string, string>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var userId = reader["AttendanceUserID"]?.ToString();
                    if (string.IsNullOrEmpty(userId))
                        continue;

                    staffNames[userId] = reader["FullName"]?.ToString() ?? "";
                }
            }
            catch (SqlException ex)
            {
                _logger.Error("Database connection error while reading staff names", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while reading staff names", ex);
                throw;
            }

            return staffNames;
        }

        public bool TestConnection()
        {
            try
            {
                _logger.Information("Testing database connection");

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    _logger.Information("Database connection successful");
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _logger.Error("Database connection failed", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error testing database connection", ex);
                return false;
            }
        }
    }
}
