using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmseducationController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmseducationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmseducation/Index.cshtml");
        }
        //[HttpPost]
        //public IActionResult Savedata([FromBody] List<EducationModel> educationList)
        //{
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    var candidateCode = HttpContext.Session.GetInt32("CandidateCode");

        //    if (!candidateCode.HasValue)
        //    {
        //        return Json(new { success = false, message = "Candidate code not found in session." });
        //    }

        //    try
        //    {
        //        using (var conn = _dbConnection.GetErpConnection())
        //        {
        //            conn.Open();

        //            // Begin transaction
        //            using (var transaction = conn.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // Delete existing records for candidate
        //                    using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM HRMS_EMPEDUCATION WHERE CODE = @code AND COMP_CODE = @compCode", conn, transaction))
        //                    {
        //                        deleteCmd.Parameters.AddWithValue("@code", candidateCode.Value);
        //                        deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
        //                        deleteCmd.ExecuteNonQuery();
        //                    }

        //                    // Insert new records
        //                    foreach (var item in educationList)
        //                    {
        //                        using (var cmd = new SqlCommand("USP_InsertEmployeeEducation", conn, transaction))
        //                        {
        //                            cmd.CommandType = CommandType.StoredProcedure;

        //                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                            cmd.Parameters.AddWithValue("@CODE", candidateCode.Value);
        //                            cmd.Parameters.AddWithValue("@M_TYPE", "HREM");
        //                            cmd.Parameters.AddWithValue("@EDUCATION", string.IsNullOrEmpty(item.Education) ? DBNull.Value : (object)item.Education);
        //                            cmd.Parameters.AddWithValue("@STREAM", string.IsNullOrEmpty(item.Stream) ? DBNull.Value : (object)item.Stream);
        //                            cmd.Parameters.AddWithValue("@BOARD_UNIVERSITY", string.IsNullOrEmpty(item.Board) ? DBNull.Value : (object)item.Board);
        //                            cmd.Parameters.AddWithValue("@YEAR", item.Year);
        //                            cmd.Parameters.AddWithValue("@RESULT", string.IsNullOrEmpty(item.Result) ? DBNull.Value : (object)item.Result);
        //                            cmd.Parameters.AddWithValue("@PERCENTAGE", item.Percentage);
        //                            cmd.Parameters.AddWithValue("@DIVISION", string.IsNullOrEmpty(item.Division) ? DBNull.Value : (object)item.Division);
        //                            cmd.Parameters.AddWithValue("@SNO", DBNull.Value); // Optional
        //                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
        //                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@AED", "A");
        //                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? string.Empty);
        //                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? string.Empty);
        //                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? string.Empty);
        //                            cmd.Parameters.AddWithValue("@Action", "Insert");

        //                            cmd.ExecuteNonQuery();
        //                        }
        //                    }
        //                    MarkStepComplete(candidateCode.Value);
        //                    // Commit transaction if all successful
        //                    transaction.Commit();

        //                    // Mark step complete once after commit


        //                    return Json(new { success = true });
        //                }
        //                catch (Exception exTrans)
        //                {
        //                    // Rollback if any error occurs
        //                    transaction.Rollback();
        //                    return Json(new { success = false, message = $"Transaction error: {exTrans.Message}" });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = $"Connection error: {ex.Message}" });
        //    }
        //}

        [HttpPost]
        public IActionResult Savedata([FromBody] EducationDataWrapper data)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            int candidateCode = data.Code > 0 ? data.Code : HttpContext.Session.GetInt32("CandidateCode") ?? 0;

            if (candidateCode == 0)
            {
                return Json(new { success = false, message = "Candidate code not found." });
            }

            if (data.EducationList == null || !data.EducationList.Any())
            {
                return Json(new { success = false, message = "No education records to save." });
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
                            //Delete existing records
                            using (var deleteCmd = new SqlCommand("DELETE FROM HRMS_EMPEDUCATION WHERE CODE = @code AND COMP_CODE = @compCode", conn, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@code", candidateCode);
                                deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
                                deleteCmd.ExecuteNonQuery();
                            }

                            // Insert new records
                            foreach (var item in data.EducationList)
                            {
                                if (string.IsNullOrWhiteSpace(item.Education)) continue;

                                using (var cmd = new SqlCommand("USP_InsertEmployeeEducation", conn, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@CODE", candidateCode);
                                    cmd.Parameters.AddWithValue("@M_TYPE", "HREM");
                                    cmd.Parameters.AddWithValue("@EDUCATION", string.IsNullOrEmpty(item.Education) ? DBNull.Value : (object)item.Education);
                                    cmd.Parameters.AddWithValue("@STREAM", string.IsNullOrEmpty(item.Stream) ? DBNull.Value : (object)item.Stream);
                                    cmd.Parameters.AddWithValue("@BOARD_UNIVERSITY", string.IsNullOrEmpty(item.Board) ? DBNull.Value : (object)item.Board);
                                    cmd.Parameters.AddWithValue("@YEAR", item.Year);
                                    cmd.Parameters.AddWithValue("@RESULT", string.IsNullOrEmpty(item.Result) ? DBNull.Value : (object)item.Result);
                                    cmd.Parameters.AddWithValue("@PERCENTAGE", item.Percentage);
                                    cmd.Parameters.AddWithValue("@DIVISION", string.IsNullOrEmpty(item.Division) ? DBNull.Value : (object)item.Division);
                                    cmd.Parameters.AddWithValue("@SNO", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? string.Empty);
                                    cmd.Parameters.AddWithValue("@Action", "Insert");

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            MarkStepComplete(candidateCode);
                            transaction.Commit();

                            return Json(new { success = true, message = "Data saved successfully." });
                        }
                        catch (Exception exTrans)
                        {
                            transaction.Rollback();
                            return Json(new { success = false, message = "Transaction error", error = exTrans.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Connection error", error = ex.Message });
            }
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
            string stepName = "hrmseducation";
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
        public IActionResult GetEducationDetailList(int code)
        {
            var educationDetails = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("USP_InsertEmployeeEducation", con))
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
                            educationDetails.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                EDUCATION = reader["EDUCATION"]?.ToString(),
                                STREAM = reader["STREAM"]?.ToString(),
                                BOARD_UNIVERSITY = reader["BOARD_UNIVERSITY"]?.ToString(),
                                YEAR = reader["YEAR"]?.ToString(),
                                RESULT = reader["RESULT"]?.ToString(),
                                PERCENTAGE = reader["PERCENTAGE"]?.ToString(),
                                DIVISION = reader["DIVISION"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new { educationDetails });
        }

        public class EducationModel
        {
            public string Code { get; set; }
            public string Education { get; set; }
            public string? Stream { get; set; }
            public string Board { get; set; }
            public int Year { get; set; }
            public string? Result { get; set; }
            public decimal? Percentage { get; set; }
            public string? Division { get; set; }
        }

        public class EducationDataWrapper
        {
            public string Action { get; set; }
            public int Code { get; set; }
            public List<EducationModel> EducationList { get; set; }
        }

    }
}
