using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsinterviewController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmsinterviewController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
           
            return View("~/Views/HRMS/hrmsinterview/Index.cshtml");
        }
        //==================================================================
        public JsonResult GetddlFirstDepartment()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select distinct Code, Name From DEPT_MAST where COMP_CODE={gv.PubCompCode} ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlFirstDesignation()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select distinct Code, Name From DESG_MAST where COMP_CODE={gv.PubCompCode} ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlFinalDepartment()
        {
            //string query = $@"Select distinct Code, Name From DEPT_MAST ORDER BY Name ASC";
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select distinct Code, Name From DEPT_MAST where COMP_CODE={gv.PubCompCode} ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlFinalDesignation()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select distinct Code, Name From DESG_MAST where COMP_CODE={gv.PubCompCode} ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //==================================================================

        public JsonResult GetddlInterviewerName()
        {
            string query = $@"Select  DISTINCT code, Name From EMP_MAST where TYPE in('STAFF') and  RESIGN_DATE <>'' order by name asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlInterviewerNameFinal()
        {
            string query = $@"Select  DISTINCT code, Name From EMP_MAST where TYPE in('STAFF') and  RESIGN_DATE <>'' order by name asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public IActionResult SaveInterviewData([FromBody] InterviewModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            // Use model.Code if provided, else fallback to session
            var candidateCode = model.Code != 0 ? model.Code : HttpContext.Session.GetInt32("CandidateCode");

            if (!candidateCode.HasValue)
            {
                return Json(new { success = false, message = "CandidateCode not found in session." });
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
                            if (model.Action.Equals("Update", StringComparison.OrdinalIgnoreCase))
                            {
                                // UPDATE existing record
                                using (var cmd = new SqlCommand("USP_InsertInterviewData", conn, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@CODE", candidateCode.Value);
                                    cmd.Parameters.AddWithValue("@M_TYPE", "HREM");

                                    cmd.Parameters.AddWithValue("@FirstLevelDate", ParseNullableDate(model.FirstLevelDate));
                                    cmd.Parameters.AddWithValue("@FirstLevelName", DbValue(model.FirstLevelName));
                                    cmd.Parameters.AddWithValue("@FirstLevelComments", DbValue(model.FirstLevelComments));
                                    cmd.Parameters.AddWithValue("@FirstLavelFeedback", DbValue(model.FirstLevelFeedback));
                                    //cmd.Parameters.AddWithValue("@FirstRecommendedforRole", DbValue(model.FirstRecommendedforRole));

                                    cmd.Parameters.AddWithValue("@FirstDepartment", DbValue(model.FirstDepartment));
                                    cmd.Parameters.AddWithValue("@FirstDesignation", DbValue(model.FirstDesignation));
                                    cmd.Parameters.AddWithValue("@FirstRecommendedLocation", DbValue(model.FirstRecommendedLocation));

                                    cmd.Parameters.AddWithValue("@FinalLevelDate", ParseNullableDate(model.FinalLevelDate));
                                    cmd.Parameters.AddWithValue("@FinalLevelName", DbValue(model.FinalLevelName));
                                    cmd.Parameters.AddWithValue("@FinalLevelComments", DbValue(model.FinalLevelComments));
                                    cmd.Parameters.AddWithValue("@FinalLavelFeedback", DbValue(model.FinalLevelFeedback));
                                    //cmd.Parameters.AddWithValue("@FinalRecommendedforRole", DbValue(model.FinalRecommendedforRole));
                                    cmd.Parameters.AddWithValue("@FinalDepartment", DbValue(model.FinalDepartment));
                                    cmd.Parameters.AddWithValue("@FinalDesignation", DbValue(model.FinalDesignation));
                                    cmd.Parameters.AddWithValue("@FinalRecommendedLocation", DbValue(model.FinalRecommendedLocation));

                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@ACTIVE", 1);
                                    cmd.Parameters.AddWithValue("@Action", "Update");

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // INSERT new record (similar to your current logic)
                                using (var cmd = new SqlCommand("USP_InsertInterviewData", conn, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@CODE", candidateCode.Value);
                                    cmd.Parameters.AddWithValue("@M_TYPE", "HREM");

                                    cmd.Parameters.AddWithValue("@FirstLevelDate", ParseNullableDate(model.FirstLevelDate));
                                    cmd.Parameters.AddWithValue("@FirstLevelName", DbValue(model.FirstLevelName));
                                    cmd.Parameters.AddWithValue("@FirstLevelComments", DbValue(model.FirstLevelComments));
                                    cmd.Parameters.AddWithValue("@FirstLavelFeedback", DbValue(model.FirstLevelFeedback));
                                    //cmd.Parameters.AddWithValue("@FirstRecommendedforRole", DbValue(model.FirstRecommendedforRole));
                                    cmd.Parameters.AddWithValue("@FirstDepartment", DbValue(model.FirstDepartment));
                                    cmd.Parameters.AddWithValue("@FirstDesignation", DbValue(model.FirstDesignation));


                                    cmd.Parameters.AddWithValue("@FirstRecommendedLocation", DbValue(model.FirstRecommendedLocation));

                                    cmd.Parameters.AddWithValue("@FinalLevelDate", ParseNullableDate(model.FinalLevelDate));
                                    cmd.Parameters.AddWithValue("@FinalLevelName", DbValue(model.FinalLevelName));
                                    cmd.Parameters.AddWithValue("@FinalLevelComments", DbValue(model.FinalLevelComments));
                                    cmd.Parameters.AddWithValue("@FinalLavelFeedback", DbValue(model.FinalLevelFeedback));
                                    //cmd.Parameters.AddWithValue("@FinalRecommendedforRole", DbValue(model.FinalRecommendedforRole));
                                    cmd.Parameters.AddWithValue("@FinalDepartment", DbValue(model.FinalDepartment));
                                    cmd.Parameters.AddWithValue("@FinalDesignation", DbValue(model.FinalDesignation));
                                    cmd.Parameters.AddWithValue("@FinalRecommendedLocation", DbValue(model.FinalRecommendedLocation));

                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@ACTIVE", 1);
                                    cmd.Parameters.AddWithValue("@Action", "Insert");

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            MarkStepComplete(candidateCode.Value);
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
        private object ParseNullableDate(string dateString)
        {
            return string.IsNullOrWhiteSpace(dateString)
                ? DBNull.Value
                : DateTime.TryParse(dateString, out var dt) ? (object)dt : DBNull.Value;
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
            string stepName = "hrmsinterview";
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
        public IActionResult GetInterviewDetailList(int code)
        {
            var interviewDetail = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("USP_InsertInterviewData", con))
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
                            interviewDetail.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                FirstLevelDate = reader["FirstLevelDate"] != DBNull.Value ? Convert.ToDateTime(reader["FirstLevelDate"]) : (DateTime?)null,
                                FirstLevelName = reader["FirstLevelName"]?.ToString(),
                                FirstLevelComments = reader["FirstLevelComments"]?.ToString(),
                                FirstLavelFeedback = reader["FirstLavelFeedback"]?.ToString(),
                                FirstDesignation = reader["FirstDesignation"]?.ToString(),
                                FirstDepartment = reader["FirstDepartment"]?.ToString(),

                                FirstRecommendedLocation = reader["FirstRecommendedLocation"]?.ToString(),

                                FinalLevelDate = reader["FinalLevelDate"] != DBNull.Value ? Convert.ToDateTime(reader["FinalLevelDate"]) : (DateTime?)null,
                                FinalLevelName = reader["FinalLevelName"]?.ToString(),
                                FinalLevelComments = reader["FinalLevelComments"]?.ToString(),
                                FinalLavelFeedback = reader["FinalLavelFeedback"]?.ToString(),

                                FinalDesignation = reader["FinalDesignation"]?.ToString(),
                                FinalDepartment = reader["FinalDepartment"]?.ToString(),
                                FinalRecommendedLocation = reader["FinalRecommendedLocation"]?.ToString(),

                                //FinalRecommendedforRole = reader["FinalRecommendedforRole"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { interviewDetail });
        }


        [HttpGet]
        public IActionResult Checkhrmstable(int code)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var result = new
            {
                basic = 0,
                personal = 0,
                education = 0,
                family = 0,
                reference = 0,
                work = 0,
                interview = 0,
                letterintent = 0,
                firstname = ""
            };

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetHRMS_TabStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            result = new
                            {
                                basic = Convert.ToInt32(dr["Basic"]),
                                personal = Convert.ToInt32(dr["Personal"]),
                                education = Convert.ToInt32(dr["Education"]),
                                family = Convert.ToInt32(dr["Family"]),
                                reference = Convert.ToInt32(dr["Reference"]),
                                work = Convert.ToInt32(dr["Work"]),
                                interview = Convert.ToInt32(dr["Interview"]),
                                letterintent = Convert.ToInt32(dr["LetterIntent"]),
                                firstname = dr["FirstName"]?.ToString() ?? ""
                            };
                        }
                    }
                }
            }
            return Json(result);
        }

        public class InterviewModel
        {
            public int Code { get; set; }  
            public string Action { get; set; }
            public string FirstLevelDate { get; set; }
            public string FirstLevelName { get; set; }
            public string FirstLevelComments { get; set; }
            public int? FirstLevelFeedback { get; set; }
            public int? FirstDesignation { get; set; }
            public int? FirstDepartment { get; set; }
            public string FirstRecommendedLocation { get; set; }
            public string FinalLevelDate { get; set; }
            public string FinalLevelName { get; set; }
            public string FinalLevelComments { get; set; }
            public int? FinalLevelFeedback { get; set; }
            public int? FinalDesignation { get; set; }
            public int? FinalDepartment { get; set; }
            public string FinalRecommendedLocation { get; set; }
        }
    }
}
