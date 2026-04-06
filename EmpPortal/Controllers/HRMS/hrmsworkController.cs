using AngleSharp.Dom;
using Dapper;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;


namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsworkController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmsworkController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/HRMS/hrmswork/Index.cshtml");
        }

        //[HttpPost]
        //public IActionResult SaveWorkExperience([FromBody] List<WorkExperienceModel> workList)
        //{
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    var candidateCode = HttpContext.Session.GetInt32("CandidateCode");

        //    // Null check for session value
        //    if (!candidateCode.HasValue)
        //    {
        //        return Json(new { success = false, message = "CandidateCode not found in session." });
        //    }

        //    if (workList == null || workList.Count == 0)
        //    {
        //        return Json(new { success = false, message = "Work experience list is empty." });
        //    }

        //    try
        //    {
        //        using (var conn = _dbConnection.GetErpConnection())
        //        {
        //            conn.Open();
        //            using (var transaction = conn.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // Step 1: Delete existing records
        //                    using (var deleteCmd = new SqlCommand(
        //                        "DELETE FROM HRMS_EMPWORK WHERE CODE = @code AND COMP_CODE = @compCode", conn, transaction))
        //                    {
        //                        deleteCmd.Parameters.AddWithValue("@code", candidateCode.Value);
        //                        deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
        //                        deleteCmd.ExecuteNonQuery();
        //                    }

        //                    // Step 2: Insert new records
        //                    foreach (var item in workList)
        //                    {
        //                        using (var cmd = new SqlCommand("USP_InsertEmployeeWorkExperience", conn, transaction))
        //                        {
        //                            cmd.CommandType = CommandType.StoredProcedure;

        //                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                            cmd.Parameters.AddWithValue("@CODE", candidateCode.Value);
        //                            cmd.Parameters.AddWithValue("@M_TYPE", "HREM");

        //                            cmd.Parameters.AddWithValue("@ORGANIZATION", DbValue(item.Organization));
        //                            cmd.Parameters.AddWithValue("@LOCATION", DbValue(item.Location));
        //                            cmd.Parameters.AddWithValue("@DESIGNATION", DbValue(item.Designation));
        //                            cmd.Parameters.AddWithValue("@DEPARTMENT", DbValue(item.Department));
        //                            cmd.Parameters.AddWithValue("@ANNUAL_CTC", DbValue(item.CTC));
        //                            cmd.Parameters.AddWithValue("@DURATION_FROM", DbValue(item.DurationFrom));
        //                            cmd.Parameters.AddWithValue("@DURATION_TO", DbValue(item.DurationTo));
        //                            cmd.Parameters.AddWithValue("@REASON_FORLEAVING", DbValue(item.Reason));

        //                            // Optional/default parameters
        //                            cmd.Parameters.AddWithValue("@SNO", DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);

        //                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
        //                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                            cmd.Parameters.AddWithValue("@AED", "A");
        //                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? string.Empty);
        //                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? string.Empty);
        //                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? string.Empty);
        //                            cmd.Parameters.AddWithValue("@Action", "Insert");

        //                            cmd.ExecuteNonQuery();
        //                        }
        //                    }

        //                    // Commit after all inserts
        //                    transaction.Commit();

        //                    // Mark step complete outside loop
        //                    MarkStepComplete(candidateCode.Value);

        //                    return Json(new { success = true });
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    return Json(new { success = false, message = $"Database transaction failed: {ex.Message}" });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = $"Unexpected error: {ex.Message}" });
        //    }
        //}

        [HttpPost]
        public IActionResult SaveWorkExperience([FromBody] WorkExperienceDataWrapper data)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            int candidateCode = data.Code > 0 ? data.Code : HttpContext.Session.GetInt32("CandidateCode") ?? 0;

            if (candidateCode == 0)
            {
                return Json(new { success = false, message = "Candidate code not found." });
            }

            if (data.WorkList == null || data.WorkList.Count == 0)
            {
                return Json(new { success = false, message = "Work experience list is empty." });
            }

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Delete existing records for candidateCode
                            using (var deleteCmd = new SqlCommand("DELETE FROM HRMS_EMPWORK WHERE CODE = @code AND COMP_CODE = @compCode", conn, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@code", candidateCode);
                                deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
                                deleteCmd.ExecuteNonQuery();
                            }

                            // Insert new records
                            foreach (var item in data.WorkList)
                            {
                                using (var cmd = new SqlCommand("USP_InsertEmployeeWorkExperience", conn, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@CODE", candidateCode);
                                    cmd.Parameters.AddWithValue("@M_TYPE", "HREM");

                                    cmd.Parameters.AddWithValue("@ORGANIZATION", DbValue(item.Organization));
                                    cmd.Parameters.AddWithValue("@LOCATION", DbValue(item.Location));
                                    cmd.Parameters.AddWithValue("@DESIGNATION", DbValue(item.Designation));
                                    cmd.Parameters.AddWithValue("@DEPARTMENT", DbValue(item.Department));
                                    cmd.Parameters.AddWithValue("@ANNUAL_CTC", DbValue(item.CTC));
                                    cmd.Parameters.AddWithValue("@DURATION_FROM", DbValue(item.DurationFrom));
                                    cmd.Parameters.AddWithValue("@DURATION_TO", DbValue(item.DurationTo));
                                    cmd.Parameters.AddWithValue("@REASON_FORLEAVING", DbValue(item.Reason));

                                    // Optional/default params
                                    cmd.Parameters.AddWithValue("@SNO", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);

                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@Action", "Insert");

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            MarkStepComplete(candidateCode);

                            return Json(new { success = true });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { success = false, message = $"Transaction failed: {ex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }


        private object DbValue(object value)
        {
            return value ?? DBNull.Value;
        }


        [HttpPost]
        public async Task<IActionResult> MarkStepComplete(int code)
        {
            try
            {
                await MarkStepCompleteInternal(code);
                return Ok(new { success = true, message = "hrmshome step marked as completed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        private async Task MarkStepCompleteInternal(int code)
        {
            string stepName = "hrmswork";
            var globalVar = _globalVariableService.GetGlobalVariables();
            await using var conn = _dbConnection.GetErpConnection();
            await conn.OpenAsync();

            string query = @"MERGE HRMS_CandidateStepStatus AS target USING (SELECT @Code AS Code, @StepName AS StepName) AS source
            ON (target.Code = source.Code AND target.StepName = source.StepName) WHEN MATCHED THEN UPDATE SET IsComplete = 1, CompletedDate = GETDATE()
            WHEN NOT MATCHED THEN INSERT (Code, StepName, IsComplete, CompletedDate, COMP_CODE) 
            VALUES (@Code, @StepName, 1, GETDATE(), @COMP_CODE);";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@Code", SqlDbType.Int).Value = code;
            cmd.Parameters.Add("@StepName", SqlDbType.NVarChar, 50).Value = stepName;
            cmd.Parameters.Add("@COMP_CODE", SqlDbType.NVarChar, 50).Value = globalVar.PubCompCode ?? (object)DBNull.Value;

            await cmd.ExecuteNonQueryAsync();
        }
        [HttpGet]
        public IActionResult GetStepStatus(int code)
        {
            var statusList = new List<object>();

            using (var conn = _dbConnection.GetErpConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT StepName, IsComplete FROM HRMS_CandidateStepStatus WHERE Code = @Code", conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            statusList.Add(new
                            {
                                StepName = reader["StepName"].ToString(),
                                IsComplete = Convert.ToBoolean(reader["IsComplete"])
                            });
                        }
                    }
                }
            }

            return Ok(statusList);
        }

        [HttpGet]
        public IActionResult GetWorkDetailList(int code)
        {
            var workDetails = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("USP_InsertEmployeeWorkExperience", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Showdata");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", code);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            workDetails.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                ORGANIZATION = reader["ORGANIZATION"]?.ToString(),
                                LOCATION = reader["LOCATION"]?.ToString(),
                                DESIGNATION = reader["DESIGNATION"]?.ToString(),
                                DEPARTMENT = reader["DEPARTMENT"]?.ToString(),
                                ANNUAL_CTC = reader["ANNUAL_CTC"]?.ToString(),
                                DURATION_FROM = reader["DURATION_FROM"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["DURATION_FROM"])
                                    : (DateTime?)null,
                                DURATION_TO = reader["DURATION_TO"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["DURATION_TO"])
                                    : (DateTime?)null,
                                REASON_FORLEAVING = reader["REASON_FORLEAVING"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new { workDetails });
        }

        public class WorkExperienceDataWrapper
        {
            public int Code { get; set; }
            public string Action { get; set; }
            public List<WorkExperienceModel> WorkList { get; set; }
        }

        public class WorkExperienceModel
        {
            public int? Code { get; set; }
            public string Organization { get; set; }
            public string Location { get; set; }
            public string Designation { get; set; }
            public string Department { get; set; }
            public decimal? CTC { get; set; }
            public DateTime? DurationFrom { get; set; }
            public DateTime? DurationTo { get; set; }
            public string Reason { get; set; }
        }







    }
}
