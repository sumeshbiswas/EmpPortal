using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsreferenceController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmsreferenceController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmsreference/Index.cshtml");
        }
        //[HttpPost]
        //public IActionResult SaveData([FromBody] List<ReferenceModel> referenceList)
        //{
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    var candidateCode = HttpContext.Session.GetInt32("CandidateCode");

        //    // Validate session and input
        //    if (!candidateCode.HasValue)
        //    {
        //        return Json(new { success = false, message = "CandidateCode not found in session." });
        //    }

        //    if (referenceList == null || referenceList.Count == 0)
        //    {
        //        return Json(new { success = false, message = "Reference list is empty." });
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
        //                    // Delete existing references for this candidate
        //                    using (var deleteCmd = new SqlCommand(
        //                        "DELETE FROM HRMS_EMPREFERENCE WHERE CODE = @code AND COMP_CODE = @compCode", conn, transaction))
        //                    {
        //                        deleteCmd.Parameters.AddWithValue("@code", candidateCode.Value);
        //                        deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
        //                        deleteCmd.ExecuteNonQuery();
        //                    }

        //                    // Insert new references
        //                    foreach (var item in referenceList)
        //                    {
        //                        using (var cmd = new SqlCommand("USP_InsertEmployeeReference", conn, transaction))
        //                        {
        //                            cmd.CommandType = CommandType.StoredProcedure;

        //                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                            cmd.Parameters.AddWithValue("@CODE", candidateCode.Value);
        //                            cmd.Parameters.AddWithValue("@M_TYPE", "HREM");

        //                            cmd.Parameters.AddWithValue("@REF_NAME", DbValue(item.Name));
        //                            cmd.Parameters.AddWithValue("@REF_ADDRESS", DbValue(item.Address));
        //                            cmd.Parameters.AddWithValue("@REF_WORK", DbValue(item.NatureOfWork));
        //                            cmd.Parameters.AddWithValue("@REF_ORGANIZATION", DbValue(item.Organization));
        //                            cmd.Parameters.AddWithValue("@REF_DESIGNATION", DbValue(item.Designation));
        //                            cmd.Parameters.AddWithValue("@REF_CONTACTNO", DbValue(item.ContactNo));
        //                            cmd.Parameters.AddWithValue("@REF_EMAIL", DbValue(item.Email));

        //                            // Optional fields
        //                            cmd.Parameters.AddWithValue("@SNO", DBNull.Value);
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

        //                    transaction.Commit();

        //                    // Mark completion after success
        //                    MarkStepComplete(candidateCode.Value);

        //                    return Json(new { success = true });
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    return Json(new { success = false, message = $"Transaction failed: {ex.Message}" });
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
        public IActionResult SaveData([FromBody] ReferenceDataWrapper data)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            int candidateCode = data.Code > 0 ? data.Code : HttpContext.Session.GetInt32("CandidateCode") ?? 0;

            if (candidateCode == 0)
            {
                return Json(new { success = false, message = "Candidate code not found." });
            }

            if (data.ReferenceList == null || data.ReferenceList.Count == 0)
            {
                return Json(new { success = false, message = "Reference list is empty." });
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
                            // Delete existing references for candidateCode
                            using (var deleteCmd = new SqlCommand(
                                "DELETE FROM HRMS_EMPREFERENCE WHERE CODE = @code AND COMP_CODE = @compCode", conn, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@code", candidateCode);
                                deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
                                deleteCmd.ExecuteNonQuery();
                            }

                            // Insert new references
                            foreach (var item in data.ReferenceList)
                            {
                                using (var cmd = new SqlCommand("USP_InsertEmployeeReference", conn, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@CODE", candidateCode);
                                    cmd.Parameters.AddWithValue("@M_TYPE", "HREM");

                                    cmd.Parameters.AddWithValue("@REF_NAME", DbValue(item.Name));
                                    cmd.Parameters.AddWithValue("@REF_ADDRESS", DbValue(item.Address));
                                    cmd.Parameters.AddWithValue("@REF_WORK", DbValue(item.NatureOfWork));
                                    cmd.Parameters.AddWithValue("@REF_ORGANIZATION", DbValue(item.Organization));
                                    cmd.Parameters.AddWithValue("@REF_DESIGNATION", DbValue(item.Designation));
                                    cmd.Parameters.AddWithValue("@REF_CONTACTNO", DbValue(item.ContactNo));
                                    cmd.Parameters.AddWithValue("@REF_EMAIL", DbValue(item.Email));

                                    // Optional fields
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

                            transaction.Commit();

                            // Mark step complete
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
        // Helper for DBNull
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
            string stepName = "hrmsreference";
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
        public IActionResult GetReferenceDetailList(int code)
        {
            var referenceDetail = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("USP_InsertEmployeeReference", con))
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
                            referenceDetail.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                REF_NAME = reader["REF_NAME"]?.ToString(),
                                REF_ADDRESS = reader["REF_ADDRESS"]?.ToString(),
                                REF_WORK = reader["REF_WORK"]?.ToString(),
                                REF_ORGANIZATION = reader["REF_ORGANIZATION"]?.ToString(),
                                REF_DESIGNATION = reader["REF_DESIGNATION"]?.ToString(),
                                REF_CONTACTNO = reader["REF_CONTACTNO"]?.ToString(),
                                REF_EMAIL = reader["REF_EMAIL"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new { referenceDetail });
        }

        public class ReferenceModel
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string NatureOfWork { get; set; }
            public string Organization { get; set; }
            public string Designation { get; set; }
            public string ContactNo { get; set; }
            public string Email { get; set; }
        }
        public class ReferenceDataWrapper
        {
            public int Code { get; set; }
            public string Action { get; set; }
            public List<ReferenceModel> ReferenceList { get; set; }
        }


    }
}
