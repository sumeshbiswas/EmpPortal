using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.GlobalErrorHandlingMiddleware
{
    public class ErrorLoggerService
    {
        private readonly DataBaseConnection _dbConnection;

        public ErrorLoggerService(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public void LogError(Exception ex, string source)
        {
            using var con = _dbConnection.GetErpConnection();
            var cmd = new SqlCommand(@"
            INSERT INTO ErrorLog (ErrorMessage, StackTrace, Source, LogDate)
            VALUES (@ErrorMessage, @StackTrace, @Source, @LogDate)", con);

            cmd.Parameters.AddWithValue("@ErrorMessage", ex.Message);
            cmd.Parameters.AddWithValue("@StackTrace", ex.StackTrace ?? "");
            cmd.Parameters.AddWithValue("@Source", source);
            cmd.Parameters.AddWithValue("@LogDate", DateTime.Now);

            con.Open();
            cmd.ExecuteNonQuery();
        }
    }

}
