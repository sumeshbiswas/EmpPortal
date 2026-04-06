
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.EmployeePortal;

namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class TimesheetDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/TimesheetDashboard/Index.cshtml");
        }

        private readonly DataBaseConnection _dbConnection;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public TimesheetDashboardController(DataBaseConnection dbConnection,
           travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;  
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
            try
            {

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {

                    con.Open();

                    int year = DateTime.Now.Year;
                    int month = DateTime.Now.Month;

                    int startYear, endYear;

                    if (month >= 4)
                    {
                        startYear = year;
                        endYear = year + 1;
                    }
                    else 
                    {
                        startYear = year - 1;
                        endYear = year;
                    }

                    string prefixYR = (startYear % 100).ToString("D2") +
                                      (endYear % 100).ToString("D2");

                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM TimeSheet WHERE COMP_CODE = @CompCode";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", 1);         
                    object result = lastVnoCmd.ExecuteScalar();

                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        [HttpPost]
        public IActionResult SavedData([FromBody] TimeSheet_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = SubmitRequest(request.Header, request.Attachment, action);

            return result == "Success" ? Json(new { success = true }) : Json(new { success = false, message = result });
        }
        private string SubmitRequest(Timesheet_Header header, List<Timesheet_Attachment> Attachments, string action)
        {
            try
            {
                using var conn = _dbConnection.GetErpConnection();    
                conn.Open();

                string compCode = HttpContext.Session.GetString("COMP_CODE");
                int? empId = HttpContext.Session.GetInt32("EMP_ID");

                using (var cmd = new SqlCommand("sp_Timesheet", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ACTION", header.action);
                    cmd.Parameters.AddWithValue("@SUBACTION", "HEADER");
                    cmd.Parameters.AddWithValue("@TaskTitle", header.TaskTitle);
                    cmd.Parameters.AddWithValue("@TaskDescription", header.TaskDescription);
                    cmd.Parameters.AddWithValue("@Priority", header.Priority);
                    cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EndDate", header.EndDate);
                    cmd.Parameters.AddWithValue("@AssignedToID", header.AssignedToID);
                    cmd.Parameters.AddWithValue("@CCTOID", header.CCTOID);
                    cmd.Parameters.AddWithValue("@BCCTOID", header.BCCTOID);
                    cmd.Parameters.AddWithValue("@AssignedByID", empId);
                    cmd.Parameters.AddWithValue("@AssignedToReply", header.AssignedToReply);
                    cmd.Parameters.AddWithValue("@AssignedByReply", header.AssignedByReply);
                    cmd.Parameters.AddWithValue("@DURATION", header.DURATION);
                    cmd.Parameters.AddWithValue("@Status", header.Status);
                    cmd.Parameters.AddWithValue("@V_TYPE", "TASK");
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@DOCID", "TASK" + header.V_NO);
                    cmd.Parameters.AddWithValue("@V_date", DateTime.Now);            
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@UUSER", empId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", empId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                     cmd.ExecuteNonQuery();
                }

                foreach (var Attachment in Attachments)
                {
                    if (string.IsNullOrWhiteSpace(Attachment.FileName))
                        continue;
                    using var cmd3 = new SqlCommand("sp_Timesheet", conn) { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@ACTION", header.action);
                    cmd3.Parameters.AddWithValue("@SUBACTION", "DETAILS");
                    cmd3.Parameters.AddWithValue("@FilePath", "/attachments/TimeSheet/" + (Attachment.FileName ?? ""));
                    cmd3.Parameters.AddWithValue("@FileName", Attachment.FileName);        
                    cmd3.Parameters.AddWithValue("@V_TYPE", "TASK");
                    cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd3.Parameters.AddWithValue("@DOCID", "TASK" + header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_date", DateTime.Now);                          
                    cmd3.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE",1);
                    cmd3.Parameters.AddWithValue("@UUSER", empId);
                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@EUSER", empId);
                    cmd3.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@AED", "A");
                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd3.ExecuteNonQuery();
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public JsonResult DDLUserMast()
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");

                string query = "select code , Name from emp_mast   where  active = 1    AND code <> 0 ";

                var DDLUserMast = _dropdownService.GetDropdownList(query);

                return Json(DDLUserMast);
            }
        }

        public JsonResult Dashboardcount()
        {
            string compCode = HttpContext.Session.GetString("COMP_CODE");
            int? empId = HttpContext.Session.GetInt32("EMP_ID");

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ACTION", "DashboardCount");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);   
                    cmd.Parameters.AddWithValue("@UUSER", empId);    
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
                            {
                                TotalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0,
                                CompleteCount = reader["CompleteCount"] != DBNull.Value ? Convert.ToInt32(reader["CompleteCount"]) : 0,
                                PendingCount = reader["PendingCount"] != DBNull.Value ? Convert.ToInt32(reader["PendingCount"]) : 0,
                                UpcomingCount = reader["UpcomingCount"] != DBNull.Value ? Convert.ToInt32(reader["UpcomingCount"]) : 0
                            });
                        }
                    }
                }
            }
            return Json(new
            {
                TotalCount = 0,
                CompleteCount = 0,
                PendingCount = 0,
                UpcomingCount = 0
            });
        }

        public JsonResult RECENTACTIVITY(string TYPE)
        {
            var taskList = new List<object>();
            try
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");
                int? empId = HttpContext.Session.GetInt32("EMP_ID");

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ACTION", "RecentActivities");
                        cmd.Parameters.AddWithValue("@SUBACTION", TYPE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@UUSER", empId);

                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                taskList.Add(new
                                {
                                    TaskTitle = reader["TaskTitle"] != DBNull.Value ? reader["TaskTitle"].ToString() : "",
                                    UserName = reader["USER_NAME"] != DBNull.Value ? reader["USER_NAME"].ToString() : "",
                                    Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : "",
                                    V_date = reader["V_date"] != DBNull.Value ? reader["V_date"].ToString() : ""
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = taskList
                });
            }
            catch (Exception ex)
            {      

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public JsonResult GetDatabyId(int rowId)
        {
            try
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");
                int? empId = HttpContext.Session.GetInt32("EMP_ID");

                Timesheet_Header header = null;
                var attachmentList = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open(); 
                            
                    using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ACTION", "CREATETASKLISTDATABYID");
                        cmd.Parameters.AddWithValue("@SUBACTION", "Header");
                        cmd.Parameters.AddWithValue("@DOCID", "TASK" + rowId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", empId ?? (object)DBNull.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                header = new Timesheet_Header
                                {
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    AssignedByID = reader["AssignedByID"] != DBNull.Value ? Convert.ToInt32(reader["AssignedByID"]) : 0,
                                    AssignedToID = reader["AssignedToID"] != DBNull.Value ? Convert.ToInt32(reader["AssignedToID"]) : 0,
                                    CCTOID = reader["CCTOID"] != DBNull.Value ? Convert.ToInt32(reader["CCTOID"]) : 0,
                                    BCCTOID = reader["BCCTOID"] != DBNull.Value ? Convert.ToInt32(reader["BCCTOID"]) : 0,
                                    TaskTitle = reader["TaskTitle"]?.ToString(),
                                    TaskDescription = reader["TaskDescription"]?.ToString(),
                                    Priority = reader["Priority"]?.ToString(),
                                    Status = reader["Status"]?.ToString(),
                                    EndDate = reader["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EndDate"])
                                };
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ACTION", "CREATETASKLISTDATABYID");
                        cmd.Parameters.AddWithValue("@SUBACTION", "Attachment");
                        cmd.Parameters.AddWithValue("@DOCID", "TASK" + rowId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", empId ?? (object)DBNull.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                attachmentList.Add(new
                                {
                                    FileName = reader["FileName"] != DBNull.Value ? reader["FileName"].ToString() : ""
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = header,
                    attachments = attachmentList
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
