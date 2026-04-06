using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using travelexpensemanagement.Models;
using System;

namespace travelexpensemanagement.Controllers.Globalvariable
{
    public class GlobalVariableService
    {
        private readonly string _connectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GlobalVariableService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _connectionString = configuration.GetConnectionString("CONDATABASE");
            _httpContextAccessor = httpContextAccessor;
        }

        public UserSessionData GetGlobalVariables()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
                throw new Exception("HttpContext is null.");

            var userCode = httpContext.Session.GetString("CODE");
            var sessionYearCode = httpContext.Session.GetString("SessionYearCode");
            var sessionComp = httpContext.Session.GetString("COMP_CODE");
            var formattedDate = httpContext.Session.GetString("SessionLogindate");

            if (string.IsNullOrEmpty(userCode))
                throw new Exception("User code not found in session. Login first.");

            DateTime loginDate = DateTime.Now;
            if (!string.IsNullOrEmpty(formattedDate))
                DateTime.TryParse(formattedDate, out loginDate);

            UserSessionData sessionData = null;

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"SELECT COMP_CODE, CODE, USER_NAME, USER_LEVEL, PC_NAME, LIP 
                                             FROM USER_MAST WHERE CODE = @UserCode", con))
            {
                cmd.Parameters.AddWithValue("@UserCode", userCode);
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        sessionData = new UserSessionData
                        {
                            PubCompCode = sessionComp,
                            PubUserId = reader["CODE"]?.ToString(),
                            PubUserName = reader["USER_NAME"]?.ToString(),
                            PubUserLevel = reader["USER_LEVEL"]?.ToString(),
                            PubWorkStationID = reader["PC_NAME"]?.ToString(),
                            PubLocalId = reader["LIP"]?.ToString(),
                            PubFYearCode = sessionYearCode,
                            PubBranchCode = 1,
                            PubLoginDate = loginDate,
                            PubSessiontime = DateTime.Now
                        };
                    }
                }
            }

            return sessionData;
        }
    }
}
