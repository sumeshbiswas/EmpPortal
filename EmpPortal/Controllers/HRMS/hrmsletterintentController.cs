using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.HRMS;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsletterintentController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmsletterintentController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmsletterintent/Index.cshtml");
        }
        public JsonResult GetEmployeeName()
        {
            string query = $@"Select distinct Code, Name From EMP_MAST ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlFinalDepartment()
        {
            string query = $@"Select distinct Code, Name From DEPT_MAST ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlFinalDesignation()
        {
            string query = $@"Select distinct Code, Name From DESG_MAST ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlReportingTo()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code, Name From EMP_MAST where type in('STAFF','SEMI STAFF') OR  PF_APPL ='yes' or ESI_APPL ='yes' and COMP_CODE ={ gv.PubCompCode } order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpPost]
        public JsonResult SaveIntent([FromBody] hrmsletterintentModel model)
        {
            try
            {
                if (model == null || model.Code == 0)
                {
                    return Json(new { status = false, message = "Code is required." });
                }
                var gv = _globalVariableService.GetGlobalVariables();
                var DOC_ID = "INT" + model.Code;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("USP_PAY_EMP_INTENT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar).Value = "INT";
                        cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = model.Code;

                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime)
                            .Value = DateTime.ParseExact(model.V_DATE, "dd/MM/yyyy", null);

                        cmd.Parameters.Add("@DOC_ID", SqlDbType.NVarChar).Value = DOC_ID;

                        // 🔥 CONVERT INT → STRING
                        cmd.Parameters.Add("@INTENT_NAME", SqlDbType.NVarChar, 50)
                            .Value = model.EmployeeCode.ToString();

                        cmd.Parameters.Add("@DEPT_NAME", SqlDbType.NVarChar, 150)
                            .Value = model.DepartmentCode.ToString();

                        cmd.Parameters.Add("@DESG_NAME", SqlDbType.NVarChar, 150)
                            .Value = model.DesignationCode.ToString();

                        cmd.Parameters.Add("@DISCUSSION_DATE", SqlDbType.SmallDateTime)
                            .Value = DateTime.ParseExact(model.DiscussionDate, "dd/MM/yyyy", null);

                        cmd.Parameters.Add("@EFF_DATE", SqlDbType.SmallDateTime)
                            .Value = DateTime.ParseExact(model.EffectiveDate, "dd/MM/yyyy", null);

                        cmd.Parameters.Add("@REPORTING_MANAGER", SqlDbType.Int)
                            .Value = model.ReportingManager;

                        cmd.Parameters.Add("@TAKE_HOME_SAL", SqlDbType.Decimal)
                            .Value = model.TakeHomeSalary;

                        cmd.Parameters.Add("@GROSS_SAL", SqlDbType.Decimal)
                            .Value = model.GrossSalary;

                        cmd.Parameters.Add("@ACCEPTANCE_DATE", SqlDbType.SmallDateTime)
                            .Value = DateTime.ParseExact(model.AcceptanceDate, "dd/MM/yyyy", null);

                        cmd.Parameters.Add("@JOINING_DATE", SqlDbType.SmallDateTime)
                            .Value = DateTime.ParseExact(model.JoiningDate, "dd/MM/yyyy", null);

                        cmd.Parameters.Add("@REPORT_LOCATION", SqlDbType.NVarChar)
                            .Value = model.ReportLocation;

                        cmd.Parameters.Add("@UUSER", SqlDbType.Int)
                            .Value = gv.PubUserId;

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                MarkStepComplete(model.Code);
                return Json(new { status = true, message = "Saved Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetHrmsLetterIntentDetailList(int code)
        {
            var letterIntentDetail = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@"
                   SELECT V_TYPE, V_NO, V_DATE, INTENT_NAME, DISCUSSION_DATE, DESG_NAME,
                   DEPT_NAME, EFF_DATE, REPORTING_MANAGER, TAKE_HOME_SAL, GROSS_SAL,
                   ACCEPTANCE_DATE, JOINING_DATE, REPORT_LOCATION, REPORT_TIME
                   FROM PAY_EMP_INTENT WHERE V_NO = @V_NO AND COMP_CODE = @COMP_CODE", con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@V_NO", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            letterIntentDetail.Add(new
                            {
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                INTENT_NAME = reader["INTENT_NAME"]?.ToString(),
                                DISCUSSION_DATE = reader["DISCUSSION_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DISCUSSION_DATE"]) : (DateTime?)null,
                                DESG_NAME = reader["DESG_NAME"]?.ToString(),
                                DEPT_NAME = reader["DEPT_NAME"]?.ToString(),
                                EFF_DATE = reader["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EFF_DATE"]) : (DateTime?)null,
                                REPORTING_MANAGER = reader["REPORTING_MANAGER"]?.ToString(),
                                TAKE_HOME_SAL = reader["TAKE_HOME_SAL"] != DBNull.Value ? Convert.ToDecimal(reader["TAKE_HOME_SAL"]) : 0,
                                GROSS_SAL = reader["GROSS_SAL"] != DBNull.Value ? Convert.ToDecimal(reader["GROSS_SAL"]) : 0,
                                ACCEPTANCE_DATE = reader["ACCEPTANCE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["ACCEPTANCE_DATE"]) : (DateTime?)null,
                                JOINING_DATE = reader["JOINING_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["JOINING_DATE"]) : (DateTime?)null,
                                REPORT_LOCATION = reader["REPORT_LOCATION"]?.ToString(),
                                REPORT_TIME = reader["REPORT_TIME"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { letterIntentDetail });
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
            string stepName = "hrmsletterintent";
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
        public IActionResult Getdropdownbanging(int code)
        {
            var list = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@"
            SELECT FinalLevelName, FinalDepartment, FinalDesignation 
            FROM HRMS_Interview 
            WHERE Code = @Code", con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@Code", code);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                FinalLevelName = reader["FinalLevelName"]?.ToString(),
                                FinalDepartment = reader["FinalDepartment"]?.ToString(),
                                FinalDesignation = reader["FinalDesignation"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(list);
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
                firstname = "",
                CheckInterviewData = ""
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
                                firstname = dr["FirstName"]?.ToString() ?? "",
                                CheckInterviewData = dr["CheckInterviewData"]?.ToString() ?? ""
                            };
                        }
                    }
                }
            }
            return Json(result);
        }


    }
}
