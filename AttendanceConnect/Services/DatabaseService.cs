using AttendanceConnect.Models;
using AttendanceConnect.Utilities;
using Microsoft.Data.SqlClient;

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
                _logger.Information($"Inserting {logs.Count} attendance logs to database");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.Information("Successfully connected to SQL Server database");

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

                    await connection.CloseAsync();
                }

                _logger.Information($"Successfully inserted {insertedCount}/{logs.Count} attendance logs");
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
