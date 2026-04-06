using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.HRMS;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsfamilyController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmsfamilyController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmsfamily/Index.cshtml");
        }
        public JsonResult GetddlDesignation()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select code, Name From DESG_MAST where COMP_CODE={globalVar.PubCompCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] FamilyWrapperModel data)
        {
            if (data == null || data.FamilyList == null || !data.FamilyList.Any())
            {
                return BadRequest(new { message = "No data received." });
            }

            var gv = _globalVariableService.GetGlobalVariables();

            //int candidateCode = data.Code > 0
            //    ? data.Code
            //    : HttpContext.Session.GetInt32("CandidateCode") ?? 0;
            var candidateCode = data.Code != 0 ? data.Code : HttpContext.Session.GetInt32("CandidateCode");

            if (candidateCode == 0)
            {
                return Json(new { success = false, message = "Candidate code not found." });
            }

            string mType = "HREM";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM HRMS_EMPFAMILY WHERE CODE = @code AND COMP_CODE = @compCode", con, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@code", candidateCode);
                        deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
                        deleteCmd.ExecuteNonQuery();
                    }

                    foreach (var member in data.FamilyList)
                    {
                        using (SqlCommand cmd = new SqlCommand("SP_INSERTHRMSEMPFAMILY", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", candidateCode);
                            cmd.Parameters.AddWithValue("@M_TYPE", mType);
                            cmd.Parameters.AddWithValue("@MEMBER_NAME", member.FamilyMember ?? "");
                            cmd.Parameters.AddWithValue("@RELATION", member.Relationship ?? "");
                            cmd.Parameters.AddWithValue("@AGE", member.Age ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GENDER", member.Gender ?? "");
                            cmd.Parameters.AddWithValue("@OCCUPATION", member.Occupation ?? "");
                            cmd.Parameters.AddWithValue("@DESIGNATION", member.Designation ?? "");
                            cmd.Parameters.AddWithValue("@ADDRESS", member.Address ?? "");
                            cmd.Parameters.AddWithValue("@CONTACT_NO", member.ContactNo ?? "");
                            cmd.Parameters.AddWithValue("@MINOR", member.Minor ?? "");
                            cmd.Parameters.AddWithValue("@NOMINEE", member.Nominee ?? "");
                            //cmd.Parameters.AddWithValue("@SHARE", member.Share ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHARE", member.Share.HasValue ? member.Share.Value : (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@REMARKS", member.Remarks ?? "");
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Action", "Insert");

                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    MarkStepComplete(candidateCode.Value);
                    return Ok(new { success = true, message = "Family details saved successfully!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error occurred while saving data.",
                        error = ex.Message
                    });
                }
            }
        }


        //[HttpPost]
        //public IActionResult SaveData([FromBody] List<FamilyMemberModel> familyList)
        //{
        //    if (familyList == null || !familyList.Any())
        //    {
        //        return BadRequest("No data received");
        //    }
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    var candidateCode = HttpContext.Session.GetInt32("CandidateCode");
        //    string mType = "HREM";

        //    if (!candidateCode.HasValue)
        //    {
        //        return Json(new { success = false, message = "Candidate code not found in session." });
        //    }
        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        con.Open();
        //        SqlTransaction transaction = con.BeginTransaction();
        //        try
        //        {
        //            using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM HRMS_EMPFAMILY WHERE CODE = @code AND COMP_CODE = @compCode", con, transaction))
        //            {
        //                deleteCmd.Parameters.AddWithValue("@code", candidateCode.Value);
        //                deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
        //                deleteCmd.ExecuteNonQuery();
        //            }
        //            foreach (var member in familyList)
        //            {
        //                using (SqlCommand cmd = new SqlCommand("SP_INSERTHRMSEMPFAMILY", con, transaction))
        //                {
        //                    cmd.CommandType = CommandType.StoredProcedure;

        //                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                    cmd.Parameters.AddWithValue("@CODE", candidateCode.Value);
        //                    cmd.Parameters.AddWithValue("@M_TYPE", mType);
        //                    cmd.Parameters.AddWithValue("@MEMBER_NAME", member.FamilyMember ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@RELATION", member.Relationship ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@AGE", member.Age);
        //                    cmd.Parameters.AddWithValue("@GENDER", member.Gender ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@OCCUPATION", member.Occupation ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@DESIGNATION", member.Designation ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@ADDRESS", member.Address ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@CONTACT_NO", member.ContactNo ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@MINOR", member.Minor ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@NOMINEE", member.Nominee ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@SHARE", member.Share);
        //                    cmd.Parameters.AddWithValue("@REMARKS", member.Remarks ?? string.Empty);

        //                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
        //                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@AED", "A");
        //                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@Action", "Insert");

        //                    cmd.ExecuteNonQuery();
        //                }
        //            }
        //            transaction.Commit();
        //            MarkStepComplete(candidateCode.Value);

        //            return Ok(new { message = "Success" });
        //        }
        //        catch (Exception ex)
        //        {
        //            transaction.Rollback();
        //            return StatusCode(500, new { success = false, message = "Error occurred while saving data.", error = ex.Message });
        //        }
        //    }
        //}
        //[HttpPost]
        //public IActionResult SaveData([FromBody] FamilyMemberModel data)
        //{
        //    if (data == null || data.FamilyList == null || !data.FamilyList.Any())
        //    {
        //        return BadRequest("No data received.");
        //    }

        //    var gv = _globalVariableService.GetGlobalVariables();

        //    int candidateCode = data.Code > 0 ? data.Code : HttpContext.Session.GetInt32("CandidateCode") ?? 0;

        //    if (candidateCode == 0)
        //    {
        //        return Json(new { success = false, message = "Candidate code not found." });
        //    }

        //    string mType = "HREM";

        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        con.Open();
        //        SqlTransaction transaction = con.BeginTransaction();

        //        try
        //        {
        //            // Delete existing data
        //            //using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM HRMS_EMPFAMILY WHERE CODE = @code AND COMP_CODE = @compCode", con, transaction))
        //            //{
        //            //    deleteCmd.Parameters.AddWithValue("@code", candidateCode);
        //            //    deleteCmd.Parameters.AddWithValue("@compCode", gv.PubCompCode);
        //            //    deleteCmd.ExecuteNonQuery();
        //            //}

        //            // Insert new data
        //            foreach (var member in data.FamilyList)
        //            {
        //                using (SqlCommand cmd = new SqlCommand("SP_INSERTHRMSEMPFAMILY", con, transaction))
        //                {
        //                    cmd.CommandType = CommandType.StoredProcedure;

        //                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //                    cmd.Parameters.AddWithValue("@CODE", candidateCode);
        //                    cmd.Parameters.AddWithValue("@M_TYPE", mType);
        //                    cmd.Parameters.AddWithValue("@MEMBER_NAME", member.FamilyMember ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@RELATION", member.Relationship ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@AGE", member.Age);
        //                    cmd.Parameters.AddWithValue("@GENDER", member.Gender ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@OCCUPATION", member.Occupation ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@DESIGNATION", member.Designation ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@ADDRESS", member.Address ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@CONTACT_NO", member.ContactNo ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@MINOR", member.Minor ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@NOMINEE", member.Nominee ?? string.Empty);
        //                    cmd.Parameters.AddWithValue("@SHARE", member.Share);
        //                    cmd.Parameters.AddWithValue("@REMARKS", member.Remarks ?? string.Empty);

        //                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
        //                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@AED", "A");
        //                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@Action", "Update");

        //                    cmd.ExecuteNonQuery();
        //                }
        //            }

        //            transaction.Commit();
        //            MarkStepComplete(candidateCode);

        //            return Ok(new { message = "Success" });
        //        }
        //        catch (Exception ex)
        //        {
        //            transaction.Rollback();
        //            return StatusCode(500, new { success = false, message = "Error occurred while saving data.", error = ex.Message });
        //        }
        //    }
        //}

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
            string stepName = "hrmsfamily";
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
        public IActionResult GetFamilyDetailList(int code)
        {
            var familyDetail = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("SP_INSERTHRMSEMPFAMILY", con))
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
                            familyDetail.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                MEMBER_NAME = reader["MEMBER_NAME"]?.ToString(),
                                RELATION = reader["RELATION"]?.ToString(),
                                AGE = reader["AGE"]?.ToString(),
                                GENDER = reader["GENDER"]?.ToString(),
                                OCCUPATION = reader["OCCUPATION"]?.ToString(),
                                DESIGNATION = reader["DESIGNATION"]?.ToString(),
                                ADDRESS = reader["ADDRESS"]?.ToString(),
                                CONTACT_NO = reader["CONTACT_NO"]?.ToString(),
                                MINOR = reader["MINOR"]?.ToString(),
                                NOMINEE = reader["NOMINEE"]?.ToString(),
                                SHARE = reader["SHARE"]?.ToString(),
                                REMARKS = reader["REMARKS"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new { familyDetail });
        }
    }
}
