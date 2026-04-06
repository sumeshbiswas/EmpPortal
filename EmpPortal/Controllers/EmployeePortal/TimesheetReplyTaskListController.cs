using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.EmployeePortal;


namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class TimesheetReplyTaskListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;

        public TimesheetReplyTaskListController(DataBaseConnection dbConnection,  ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;      

        }
        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/TimesheetReplyTaskList/Index.cshtml");
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

                    cmd.Parameters.AddWithValue("@Action", "REPLYTASKLIST");
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
                                AssignedBy = reader["USER_NAME"] != DBNull.Value ? reader["USER_NAME"].ToString() : string.Empty,
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

        public IActionResult Gedatabydatewise(string searchTerm = "", int pageNumber = 1, int pageSize = 10 , DateTime? startDate = null, DateTime? endTime = null)
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

                    cmd.Parameters.AddWithValue("@Action", "REPLYTASKLISTbydatefilter");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endTime);
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
                                AssignedBy = reader["USER_NAME"] != DBNull.Value ? reader["USER_NAME"].ToString() : string.Empty,
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

        public JsonResult RECENTACTIVITY(string DocID)
        {
            object replyDetail = null;
            var attachmentList = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();                
                    using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ACTION", "REPLYSHOWDATABYID");
                        cmd.Parameters.AddWithValue("@SUBACTION", "REPLYDETAIL");
                        cmd.Parameters.AddWithValue("@DOCID","TASK" + DocID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) 
                            {
                                replyDetail = new
                                {
                                    DOCID = reader["DOCID"]?.ToString() ?? "",
                                    TaskTitle = reader["TaskTitle"]?.ToString() ?? "",
                                    TaskDescription = reader["TaskDescription"]?.ToString() ?? "",
                                    USER_NAME = reader["USER_NAME"]?.ToString() ?? "",
                                    AssignedToReply = reader["AssignedToReply"]?.ToString() ?? "",
                                    Status = reader["Status"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                                       
                    using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ACTION", "REPLYSHOWDATABYID");
                        cmd.Parameters.AddWithValue("@SUBACTION", "REPLYATTACHMENT");
                        cmd.Parameters.AddWithValue("@DOCID", "TASK" + DocID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                attachmentList.Add(new
                                {                                 
                                    FileName = reader["FileName"]?.ToString() ?? "",
                                    FilePath = reader["FilePath"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, replyDetail = replyDetail, attachments = attachmentList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SavedData([FromBody] TimeSheet_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }
                 
            var result = SubmitRequest(request.Header, request.Attachment);

            return result == "Success" ? Json(new { success = true }) : Json(new { success = false, message = result });
        }
        private string SubmitRequest(Timesheet_Header header, List<Timesheet_Attachment> Attachments)
        {
            try
            {
                using var conn = _dbConnection.GetErpConnection();
                string compCode = HttpContext.Session.GetString("COMP_CODE");
                int? empId = HttpContext.Session.GetInt32("EMP_ID");
                conn.Open();

                using (var cmd = new SqlCommand("sp_Timesheet", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ACTION", "REPLYUPDATE");
                    cmd.Parameters.AddWithValue("@SUBACTION", "REPLY");             
                    cmd.Parameters.AddWithValue("@Status", header.Status);        
                    cmd.Parameters.AddWithValue("@DOCID", header.DOCID);        
                    cmd.Parameters.AddWithValue("@AssignedToReply", header.AssignedToReply);        
                    cmd.Parameters.AddWithValue("@V_DATE", DateTime.Now);        
                    cmd.ExecuteNonQuery();
                }

                string docId = header.DOCID;
                string result = docId.Replace("TASK", "");

                foreach (var Attachment in Attachments)
                {
                    if (string.IsNullOrWhiteSpace(Attachment.FileName))
                        continue;
                    using var cmd3 = new SqlCommand("sp_Timesheet", conn) { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@ACTION", "REPLYUPDATE");
                    cmd3.Parameters.AddWithValue("@SUBACTION", "ATTACHMENT");
                    cmd3.Parameters.AddWithValue("@FilePath", "/attachments/TimeSheet/" + (Attachment.FileName ?? ""));
                    cmd3.Parameters.AddWithValue("@FileName", Attachment.FileName);
                    cmd3.Parameters.AddWithValue("@V_TYPE", "TASK");
                    cmd3.Parameters.AddWithValue("@V_NO", result);
                    cmd3.Parameters.AddWithValue("@DOCID", header.DOCID);
                    cmd3.Parameters.AddWithValue("@V_date", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", 1);
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


        public JsonResult Replyhistory(int DocID)
        {
  
            var REPLYHISTORY = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
        
                    using (SqlCommand cmd = new SqlCommand("sp_Timesheet", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ACTION", "REPLYHISTORY");
                        cmd.Parameters.AddWithValue("@DOCID", "TASK" + DocID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                REPLYHISTORY.Add(new
                                {
                                    AssignedToReply = reader["AssignedToReply"]?.ToString() ?? "",
                                    Status = reader["Status"]?.ToString() ?? "",
                                    V_DATE = reader["V_DATE"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, REPLYHISTORY = REPLYHISTORY });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
