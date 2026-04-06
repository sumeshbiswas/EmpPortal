using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class EmpLeaveApprovalListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;

        public EmpLeaveApprovalListController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/EmpLeaveApprovalList/Index.cshtml");
        }

        [HttpPost]
        public IActionResult UPDATEDATA([FromBody] LeaveUpdateModel model)
        {
            try
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");

                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    string sql = @"UPDATE pay_leave 
                    SET faprov_status=@status,
                    faprov_remarks='Updated By Manager'
                    WHERE DOC_ID=@docId 
                    AND COMP_CODE=@compCode";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@docId", model.docId);
                        cmd.Parameters.AddWithValue("@status", model.status);
                        cmd.Parameters.AddWithValue("@compCode", compCode);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Json(new { success = true });
                        }

                        return Json(new { success = false, message = "Update failed" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DELETEROW([FromBody] LeaveUpdateModel model)
        {
            try
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");

                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    string sql = @"DELETE FROM pay_leave 
                           WHERE DOC_ID = @docId 
                           AND COMP_CODE = @compCode";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@docId", model.docId);
                        cmd.Parameters.AddWithValue("@compCode", compCode);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Json(new { success = true });
                        }

                        return Json(new { success = false, message = "Delete failed" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            int totalCount = 0;
            var headerList = new List<LeaveList>();

            try
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");
                int? empId = HttpContext.Session.GetInt32("EMP_ID");

                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_EmpLeaveApprovalList", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm",
                        string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@parent_desg", 10463);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new LeaveList
                            {
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                USER_NAME = reader["USER_NAME"]?.ToString(),
                                faprov_remarks = reader["faprov_remarks"]?.ToString(),
                                faprov_status = reader["faprov_status"]?.ToString(),
                                leave_type = reader["leave_type"]?.ToString(),
                                from_date = reader["from_date"] != DBNull.Value ? Convert.ToDateTime(reader["from_date"]) : (DateTime?)null,
                                to_date = reader["to_date"] != DBNull.Value ? Convert.ToDateTime(reader["to_date"]) : (DateTime?)null
                            });
                        }

                        // Second result set for total count
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value
                                ? Convert.ToInt32(reader["TotalCount"])
                                : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data.", error = ex.Message });
            }

            return Json(new { success = true, lists = headerList, totalCount });
        }

        // Proper model class
        public class LeaveList
        {
            public string DOC_ID { get; set; }

            public string USER_NAME { get; set; }

            public string faprov_remarks { get; set; }

            public string faprov_status { get; set; }

            public string leave_type { get; set; }

            public DateTime? from_date { get; set; }

            public DateTime? to_date { get; set; }
        }
        public class LeaveUpdateModel
        {
            public string docId { get; set; }
            public string status { get; set; }
        }

    }
}