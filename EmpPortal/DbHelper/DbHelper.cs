using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace travelexpensemanagement.DbHelper
{
    public class DbHelper
    {
        private readonly DataBaseConnection _dbConnection;
        public DbHelper(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public DataTable GetDataTableFromStoredProcedure(string storedProcedureName, List<SqlParameter> parameters = null)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(storedProcedureName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
            }
            return dt;
        }
        public async Task<DataTable> GetDataTableFromStoredProcedureAsync(string storedProcedureName, List<SqlParameter> parameters = null)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(storedProcedureName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing stored procedure async '{storedProcedureName}': {ex.Message}", ex);
            }
            return dt;
        }
        public async Task<DataTable> ExecuteQueryAsync(string sqlQuery, List<SqlParameter> parameters = null)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.CommandType = CommandType.Text;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing query: {ex.Message}", ex);
            }
            return dt;
        }
        public static List<T> ConvertToList<T>(DataTable dt) where T : new()
        {
            var data = new List<T>();

            foreach (DataRow row in dt.Rows)
            {
                T item = new T();

                foreach (var prop in typeof(T).GetProperties())
                {
                    if (dt.Columns.Contains(prop.Name) &&
                        row[prop.Name] != DBNull.Value &&
                        prop.CanWrite)
                    {
                        try
                        {
                            prop.SetValue(item, Convert.ChangeType(row[prop.Name], prop.PropertyType));
                        }
                        catch
                        {
                        }
                    }
                }
                data.Add(item);
            }

            return data;
        }
        public async Task<List<T>> GetListFromStoredProcedureAsync<T>(string storedProcedureName, List<SqlParameter> parameters = null) where T : new()
        {
            var dt = await GetDataTableFromStoredProcedureAsync(storedProcedureName, parameters);
            return ConvertToList<T>(dt);
        }
        //Any Data Save then use pass this parameter this code
        public void LogChange(string tableName, string operationType, string primaryKeyValue,
                       string username, string userCode, string companyCode,
                       string oldValue = null, string newValue = null)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_LogChange", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@OperationType", operationType);
                    cmd.Parameters.AddWithValue("@PrimaryKeyValue", primaryKeyValue);// EmpCode
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@UserCode", userCode);
                    cmd.Parameters.AddWithValue("@CompanyCode", companyCode);
                    cmd.Parameters.AddWithValue("@OldValue", (object?)oldValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", (object?)newValue ?? DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        //How to call this funcation _dbHelper.LogChange("Employee", "INSERT", model.EmployeeId.ToString(), "admin", "EMP001", "COMP123");
        public async Task ExecuteQueryAsynctran(string query, List<SqlParameter> parameters, SqlTransaction transaction = null)
        {
            using (var command = new SqlCommand(query, transaction.Connection, transaction))
            {
                command.Parameters.AddRange(parameters.ToArray());
                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task<T> ExecuteScalarAsynctran<T>(string query, List<SqlParameter> parameters, SqlTransaction transaction = null)
        {
            using (var command = new SqlCommand(query, transaction.Connection, transaction))
            {
                command.Parameters.AddRange(parameters.ToArray());
                var result = await command.ExecuteScalarAsync();
                return (T)Convert.ChangeType(result, typeof(T));
            }
        }
        public object Xnull(object temp)
        {
            if (temp == null || temp.ToString() == "")
            {
                return "";
            }

            return temp;

        }
        public object Vnull(object temp)
        {

            if (temp == null)
            {
                return 0;
            }

            try
            {
                double Varvalue = Convert.ToDouble(temp);

                if (Varvalue == 0)
                {
                    return 0;
                }

                return Varvalue;
            }
            catch (FormatException)
            {
                return 0;
            }

        }
        public async Task<IReadOnlyList<ExpandoObject>> GetJsonDataAsync(string sql)
        {
            var results = new List<ExpandoObject>();
            await using var con = _dbConnection.GetErpConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var row = new ExpandoObject() as IDictionary<string, object?>;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = await reader.IsDBNullAsync(i).ConfigureAwait(false)
                                ? null
                                : reader.GetValue(i);

                    row[name] = value;
                }

                results.Add((ExpandoObject)row);
            }

            return results;
        }
        public static string NotNullOrEmptyCondition(string column)
        {
            return $"ISNULL({column}, '') <> ''";
        }

        public static DateTime? ConvertToDate(object dateObj)
        {
            if (dateObj == null || dateObj == DBNull.Value)
                return null;

            if (DateTime.TryParse(dateObj.ToString(), out DateTime date))
                return date;

            return null;
        }
        // Optional: for frontend display only
        public static string FormatDateForDisplay(object dateObj)
        {
            var date = ConvertToDate(dateObj);
            return date?.ToString("dd/MM/yyyy");
        }

        public object Xnull<T>(T value)
        {
            if (value == null)
                return DBNull.Value;

            if (value is string str && string.IsNullOrWhiteSpace(str))
                return DBNull.Value;

            return value;
        }
        private List<Dictionary<string, object>> ConvertToList(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
                return new List<Dictionary<string, object>>();

            return dt.AsEnumerable()
                     .Select(row =>
                         dt.Columns.Cast<DataColumn>()
                           .ToDictionary(
                               col => col.ColumnName,
                               col =>
                               {
                                   var value = row[col];
                                   return value == DBNull.Value ? "" : value;
                               }))
                     .ToList();
        }
        public async Task<IReadOnlyList<ExpandoObject>> GetJsonFromProcedureAsync(string procedureName, Dictionary<string, object> parameters)
        {
            var results = new List<ExpandoObject>();

            try
            {
                await using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync().ConfigureAwait(false);
                using var cmd = new SqlCommand(procedureName, con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var row = new ExpandoObject() as IDictionary<string, object?>;
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var value = await reader.IsDBNullAsync(i).ConfigureAwait(false)
                                     ? null
                                     : reader.GetValue(i);
                        row[name] = value;
                    }
                    results.Add((ExpandoObject)row);
                }
                return results;
            }
            catch (Exception ex)
            {
                return Array.Empty<ExpandoObject>();
            }

        }
        public async Task<T> GetExecuteScalarAsync<T>(string queryOrProcName, Dictionary<string, object> parameters = null, bool isStoredProc = false)
        {
            using (var con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(queryOrProcName, con))
            {
                cmd.CommandType = isStoredProc ? CommandType.StoredProcedure : CommandType.Text;

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
        }
        public DateTime? FGetSmallDateTime(object inputDate)
        {
            if (inputDate == null || inputDate == DBNull.Value)
                return null;

            if (inputDate is DateTime dt)
                return dt;

            if (inputDate is string s)
            {
                string[] formats = {"dd-MMM-yyyy", "dd-MM-yyyy", "MM-dd-yyyy","yyyy-MM-dd","d-MMM-yyyy","d-MM-yyyy","M-d-yyyy",
                    "yyyy/MM/dd","MM/dd/yyyy","dd/MM/yyyy"};

                if (DateTime.TryParseExact(
                        s.Trim(),
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime parsedDate))
                {
                    return parsedDate;
                }
            }

            return null;
        }

        public void ExecuteNonQuery(string query, List<SqlParameter> parameters = null, bool isStoredProc = false)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = isStoredProc ? CommandType.StoredProcedure : CommandType.Text;

                    if (parameters != null && parameters.Count > 0)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing {(isStoredProc ? "stored procedure" : "query")}: {ex.Message}", ex);
            }
        }
        public async Task<string> ExecuteScalarAsync(string sqlQuery, List<SqlParameter> parameters = null)
        {
            string result = string.Empty; // Initialize with a non-null default value
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.CommandType = CommandType.Text;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await con.OpenAsync();
                    var scalarResult = await cmd.ExecuteScalarAsync();
                    result = scalarResult?.ToString() ?? string.Empty; // Safely handle null values
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing query: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<bool> IsDataExist(string sqlQuery, List<SqlParameter> parameters = null)
        {
            bool result = false; // Initialize with a non-null default value
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.CommandType = CommandType.Text;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await con.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();

                    if (reader.HasRows)
                    {
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing query: {ex.Message}", ex);
            }
            return result;
        }
        internal void ExecuteNonQuery(string insertQuery, List<Microsoft.Data.SqlClient.SqlParameter> insertParams)
        {
            throw new NotImplementedException();
        }

       

    }
}
