using Microsoft.Data.SqlClient;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using System;

namespace travelexpensemanagement.LogService
{
    public class LogService
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public LogService(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public void InsertLog(string tableName, string vType, string vNo, string description)
        {
            var sessionData = _globalVariableService.GetGlobalVariables();
            if (sessionData == null)
            {
                // Optionally log or handle null session
                return;
            }

            using (SqlConnection con = _dbConnection.GetErpConnection()) 
            {
                SqlCommand cmd = new SqlCommand(@" INSERT INTO LOG_TABLE
                    (COMP_CODE, TABLE_NAME, V_TYPE, V_NO, DESCRIPTION, EUSER, EDATE, WSID, LIP, LID) VALUES
                    (@COMP_CODE, @TABLE_NAME, @V_TYPE, @V_NO, @DESCRIPTION, @EUSER, @EDATE, @WSID, @LIP, @LID)", con);

                cmd.Parameters.AddWithValue("@COMP_CODE", sessionData.PubCompCode ?? "");
                cmd.Parameters.AddWithValue("@TABLE_NAME", tableName);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@DESCRIPTION", description);
                cmd.Parameters.AddWithValue("@EUSER", DateTime.Now);
                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@WSID", sessionData.PubWorkStationID ?? "");
                cmd.Parameters.AddWithValue("@LIP", sessionData.PubLocalId ?? "");
                cmd.Parameters.AddWithValue("@LID", Environment.UserName);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //string description = $"FieldName: Mobile, Old Value: 9876543210, New Value: 1234567890";
        //_logService.InsertLog("Employee", "UPDATE", "EMP123", description);

    }
}
