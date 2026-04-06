using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.EmployeePortal;
namespace travelexpensemanagement.Controllers.EmployeePortal
{

    public class TimesheetTaskListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;

        public TimesheetTaskListController(DataBaseConnection dbConnection, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;

        }

        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/TimesheetTaskList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {

            int totalCount = 0;
            var headerList = new List<Timesheet_Header>();
            string compCode = HttpContext.Session.GetString("COMP_CODE");
            int? empId = HttpContext.Session.GetInt32("EMP_ID");
            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_Timesheet", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "CRREATETASKLIST");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@UUSER", empId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new Timesheet_Header
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]) : DateTime.MinValue,
                                TaskTitle = reader["TaskTitle"] != DBNull.Value ? reader["TaskTitle"].ToString() : string.Empty,
                                DOCID = reader["DOCID"] != DBNull.Value ? reader["DOCID"].ToString() : string.Empty,
                                AssignedBy = reader["AssignedBy"] != DBNull.Value ? reader["AssignedBy"].ToString() : string.Empty,
                                Priority = reader["Priority"] != DBNull.Value ? reader["Priority"].ToString() : string.Empty,
                                Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : string.Empty
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
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


        public IActionResult GetListbystatus(string searchTerm = "", int pageNumber = 1, int pageSize = 10 , string  Status = "")
        {

            int totalCount = 0;
            var headerList = new List<Timesheet_Header>();
            string compCode = HttpContext.Session.GetString("COMP_CODE");
            int? empId = HttpContext.Session.GetInt32("EMP_ID");
            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_Timesheet", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "CRREATETASKLISTbyStatus");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@UUSER", empId);
                    cmd.Parameters.AddWithValue("@Status", Status);


                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new Timesheet_Header
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]) : DateTime.MinValue,
                                TaskTitle = reader["TaskTitle"] != DBNull.Value ? reader["TaskTitle"].ToString() : string.Empty,
                                DOCID = reader["DOCID"] != DBNull.Value ? reader["DOCID"].ToString() : string.Empty,
                                AssignedBy = reader["AssignedBy"] != DBNull.Value ? reader["AssignedBy"].ToString() : string.Empty,
                                Priority = reader["Priority"] != DBNull.Value ? reader["Priority"].ToString() : string.Empty,
                                Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : string.Empty
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
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





        public JsonResult CloseTask(int id)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");
                string DOCID = "TASK" + id;
                string query = "UPDATE TimeSheet SET Status = @Status WHERE DOCID = @DocId ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Status", "Close");
                    cmd.Parameters.AddWithValue("@DocId", DOCID);
                    cmd.Parameters.AddWithValue("@CompCode", compCode);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Task closed successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Task not found or already closed." });
                    }
                }
            }
        }

    }
}
