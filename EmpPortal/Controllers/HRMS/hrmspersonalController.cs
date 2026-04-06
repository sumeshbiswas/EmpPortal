using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmspersonalController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmspersonalController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmspersonal/Index.cshtml");
        }
        public JsonResult GetdddlAddressDetailsCity()
        {
            string query = $@" Select code, Name From CITY_MAST order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlBankName()
        {
            string query = $@" Select code,Name From BANK_MAST";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetdddlAddressDetailsState()
        {
            string query = $@" Select code, Name From STATE_MAST order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //[HttpPost]
        //public async Task<IActionResult> SaveEmpPersonal([FromForm] PersonalDetailsViewModel model)
        //{
        //    try
        //    {
        //        var globalVar = _globalVariableService.GetGlobalVariables();
        //        var compCode = globalVar.PubCompCode;
        //        var userId = globalVar.PubUserId;
        //        DateTime currentDate = DateTime.Now;
        //        var candidateCode = HttpContext.Session.GetInt32("CandidateCode");


        //        // File saving function
        //        string SaveFile(IFormFile file, string folder)
        //        {
        //            if (file != null && file.Length > 0)
        //            {
        //                string uploadsFolder = Path.Combine("wwwroot", "Uploads", folder);
        //                Directory.CreateDirectory(uploadsFolder);

        //                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        //                string fullPath = Path.Combine(uploadsFolder, fileName);

        //                using (var stream = new FileStream(fullPath, FileMode.Create))
        //                {
        //                    file.CopyTo(stream);
        //                }

        //                return Path.Combine("Uploads", folder, fileName).Replace("\\", "/");
        //            }
        //            return null;
        //        }

        //        // Save uploaded files
        //        string passportPath = SaveFile(model.PassportFile, "Passport");
        //        string drivingPath = SaveFile(model.DrivingFile, "DrivingLicense");
        //        string panPath = SaveFile(model.PanFile, "PAN");
        //        string voterPath = SaveFile(model.VoterFile, "VoterID");
        //        string aadharPath = SaveFile(model.AadharFile, "Aadhar");

        //        var parameters = new DynamicParameters();

        //        parameters.Add("@COMP_CODE", compCode);
        //        parameters.Add("@CODE", candidateCode);
        //        parameters.Add("@M_TYPE", "EMPP");
        //        parameters.Add("@DOB", model.DateOfBirth);
        //        parameters.Add("@AGE", model.Age);
        //        parameters.Add("@GENDER", ""); // Add gender to model if available

        //        parameters.Add("@PRESENT_ADD1", model.PresentAddress);
        //        parameters.Add("@PRESENT_CITY", Convert.ToInt32(model.PresentCity));
        //        parameters.Add("@PRESENT_STATE", Convert.ToInt32(model.PresentState));
        //        parameters.Add("@PRESENT_PINCODE", model.PresentPincode);
        //        parameters.Add("@SAME_ADDRESS", model.SameAsPresent ? 1 : 0);
        //        parameters.Add("@PERMANENT_ADD1", model.PermanentAddress);
        //        parameters.Add("@PERMANENT_CITY", Convert.ToInt32(model.PermanentCity));
        //        parameters.Add("@PERMANENT_STATE", Convert.ToInt32(model.PermanentState));
        //        parameters.Add("@PERMANENT_PINCODE", model.PermanentPincode);

        //        parameters.Add("@MOBILE_NO", model.MobileNo);
        //        parameters.Add("@EMERGENCY_NO", model.EmergencyNo);
        //        parameters.Add("@EMAIL_ID", model.EmailId);
        //        parameters.Add("@PASSPORT_NO", model.PassportNo);
        //        parameters.Add("@PATH_PASSPORT", passportPath);
        //        parameters.Add("@DRIVING_LICENCENO", model.DrivingLicenceNo);
        //        parameters.Add("@PATH_DL", drivingPath);

        //        parameters.Add("@MARITAL_STATUS", model.MaritalStatus);
        //        parameters.Add("@WEDDING_DATE", model.WeddingDate);
        //        parameters.Add("@BLOOD_GROUP", model.BloodGroup);
        //        parameters.Add("@PAN_NO", model.PANNo);
        //        parameters.Add("@PATH_PAN", panPath);
        //        parameters.Add("@VOTER_ID", model.VoterId);
        //        parameters.Add("@PATH_VOTERID", voterPath);
        //        parameters.Add("@AADHAR_NO", model.AadharNo);
        //        parameters.Add("@PATH_AADHAR", aadharPath);

        //        parameters.Add("@ACT_NO", model.AccountNo);
        //        parameters.Add("@IFSC_CODE", model.IFSCCode);
        //        parameters.Add("@BANK_CODE", 0); // Map from BankName if needed

        //        parameters.Add("@LANGUAGE_KNOWN", model.LanguagesKnown);
        //        parameters.Add("@HOBBIES", model.Hobbies);
        //        parameters.Add("@GOAL_LIFE", model.LifeGoal);
        //        parameters.Add("@INVOLVED_COURT", model.CourtCase);

        //        // Audit Info
        //        parameters.Add("@UUSER", userId);
        //        parameters.Add("@UDATE", currentDate);
        //        //parameters.Add("@EUSER", DBNull.Value);
        //        //parameters.Add("@EDATE", DBNull.Value);
        //        parameters.Add("@EUSER", (int?)null);
        //        parameters.Add("@EDATE", (DateTime?)null);
        //        parameters.Add("@AED", "A");
        //        parameters.Add("@WSID", globalVar.PubWorkStationID);
        //        parameters.Add("@LIP", globalVar.PubLocalId);
        //        parameters.Add("@LID", Environment.MachineName);

        //        // Paging/Searching (not used here but required by proc)
        //        parameters.Add("@Action", "Insert");

        //        string sql = "USP_INSERTHRMSEMPPERSONAL";
        //        using (var conn = _dbConnection.GetErpConnection())
        //        {
        //            await conn.ExecuteAsync("USP_INSERT_HRMS_EMPPERSONAL", parameters, commandType: CommandType.StoredProcedure);
        //        }
        //        if (candidateCode.HasValue)
        //        {
        //            await MarkStepComplete(candidateCode.Value); 
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "Candidate code not found in session." });
        //        }
        //        return Json(new { success = true, message = "Saved successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveEmpPersonal([FromForm] PersonalDetailsViewModel model)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                var compCode = globalVar.PubCompCode;
                var userId = globalVar.PubUserId;
                DateTime currentDate = DateTime.Now;
                int? candidateCode = HttpContext.Session.GetInt32("CandidateCode");
                //var candidateCode = HttpContext.Session.GetInt32("CandidateCode");

                //if (!candidateCode.HasValue)
                //{
                //    return Json(new { success = false, message = "Candidate code not found in session." });
                //}

                // Reusable file saving function
                string SaveFile(IFormFile file, string folder)
                {
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine("wwwroot", "Uploads", folder);
                        Directory.CreateDirectory(uploadsFolder);

                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        string fullPath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        return Path.Combine("Uploads", folder, fileName).Replace("\\", "/");
                    }
                    return null;
                }

                // Save uploaded files once
                string passportPath = SaveFile(model.PassportFile, "Passport");
                string drivingPath = SaveFile(model.DrivingFile, "DrivingLicense");
                string panPath = SaveFile(model.PanFile, "PAN");
                string voterPath = SaveFile(model.VoterFile, "VoterID");
                string aadharPath = SaveFile(model.AadharFile, "Aadhar");

                var parameters = new DynamicParameters();

                parameters.Add("@COMP_CODE", compCode);

                if (model.Action == "Insert")
                {
                    parameters.Add("@CODE", candidateCode.Value);
                }
                else if (model.Action == "Update")
                {
                    parameters.Add("@CODE", model.Code);
                }               
                parameters.Add("@M_TYPE", "EMPP");
                parameters.Add("@DOB", model.DateOfBirth);
                parameters.Add("@AGE", model.Age);
                parameters.Add("@GENDER", ""); // Add to model if needed

                parameters.Add("@PRESENT_ADD1", model.PresentAddress);
                parameters.Add("@PRESENT_CITY", Convert.ToInt32(model.PresentCity));
                parameters.Add("@PRESENT_STATE", Convert.ToInt32(model.PresentState));
                parameters.Add("@PRESENT_PINCODE", model.PresentPincode);
                parameters.Add("@SAME_ADDRESS", model.SameAsPresent ? 1 : 0);
                parameters.Add("@PERMANENT_ADD1", model.PermanentAddress);
                parameters.Add("@PERMANENT_CITY", Convert.ToInt32(model.PermanentCity));
                parameters.Add("@PERMANENT_STATE", Convert.ToInt32(model.PermanentState));
                parameters.Add("@PERMANENT_PINCODE", model.PermanentPincode);

                parameters.Add("@MOBILE_NO", model.MobileNo);
                parameters.Add("@EMERGENCY_NO", model.EmergencyNo);
                parameters.Add("@EMAIL_ID", model.EmailId);
                parameters.Add("@PASSPORT_NO", model.PassportNo);
                parameters.Add("@PATH_PASSPORT", passportPath);
                parameters.Add("@DRIVING_LICENCENO", model.DrivingLicenceNo);
                parameters.Add("@PATH_DL", drivingPath);

                parameters.Add("@MARITAL_STATUS", model.MaritalStatus);
                parameters.Add("@WEDDING_DATE", model.WeddingDate);
                parameters.Add("@BLOOD_GROUP", model.BloodGroup);
                parameters.Add("@PAN_NO", model.PANNo);
                parameters.Add("@PATH_PAN", panPath);
                parameters.Add("@VOTER_ID", model.VoterId);
                parameters.Add("@PATH_VOTERID", voterPath);
                parameters.Add("@AADHAR_NO", model.AadharNo);
                parameters.Add("@PATH_AADHAR", aadharPath);

                parameters.Add("@ACT_NO", model.AccountNo);
                parameters.Add("@IFSC_CODE", model.IFSCCode);
                parameters.Add("@BANK_CODE", model.BankName); // Map from BankName if needed

                parameters.Add("@LANGUAGE_KNOWN", model.LanguagesKnown);
                parameters.Add("@HOBBIES", model.Hobbies);
                parameters.Add("@GOAL_LIFE", model.LifeGoal);
                parameters.Add("@INVOLVED_COURT", model.CourtCase);

                // Audit Info
                parameters.Add("@UUSER", userId);
                parameters.Add("@UDATE", currentDate);
                parameters.Add("@EUSER", (int?)null);
                parameters.Add("@EDATE", (DateTime?)null);
                parameters.Add("@AED", "A");
                parameters.Add("@WSID", globalVar.PubWorkStationID);
                parameters.Add("@LIP", globalVar.PubLocalId);
                parameters.Add("@LID", Environment.MachineName);

                // Determine action for stored procedure
                string spAction = null;
                if (model.Action == "Insert")
                {
                    spAction = "Insert";
                }
                else if (model.Action == "Update")
                {
                    spAction = "Update";
                }
                else
                {
                    return Json(new { success = false, message = "Invalid action." });
                }

                parameters.Add("@Action", spAction);

                using (var conn = _dbConnection.GetErpConnection())
                {
                    await conn.ExecuteAsync("USP_INSERT_HRMS_EMPPERSONAL", parameters, commandType: CommandType.StoredProcedure);
                }

                await MarkStepComplete(candidateCode.Value);

                return Json(new { success = true, message = "Saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
            string stepName = "hrmspersonal";
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
        public IActionResult GetPersonalDetailList(int code)
        {
            var personalDetail = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("USP_INSERT_HRMS_EMPPERSONAL", con))
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
                            personalDetail.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                DOB = reader["DOB"] != DBNull.Value ? Convert.ToDateTime(reader["DOB"]) : (DateTime?)null,
                                AGE = reader["AGE"]?.ToString(),
                                PRESENT_ADD1 = reader["PRESENT_ADD1"]?.ToString(),
                                PRESENT_CITY = reader["PRESENT_CITY"]?.ToString(),
                                PRESENT_STATE = reader["PRESENT_STATE"]?.ToString(),
                                PRESENT_PINCODE = reader["PRESENT_PINCODE"]?.ToString(),
                                SAME_ADDRESS = reader["SAME_ADDRESS"]?.ToString(),
                                PERMANENT_ADD1 = reader["PERMANENT_ADD1"]?.ToString(),
                                PERMANENT_CITY = reader["PERMANENT_CITY"]?.ToString(),
                                PERMANENT_STATE = reader["PERMANENT_STATE"]?.ToString(),
                                PERMANENT_PINCODE = reader["PERMANENT_PINCODE"]?.ToString(),
                                MOBILE_NO = reader["MOBILE_NO"]?.ToString(),
                                EMERGENCY_NO = reader["EMERGENCY_NO"]?.ToString(),
                                EMAIL_ID = reader["EMAIL_ID"]?.ToString(),
                                PASSPORT_NO = reader["PASSPORT_NO"]?.ToString(),
                                DRIVING_LICENCENO = reader["DRIVING_LICENCENO"]?.ToString(),
                                MARITAL_STATUS = reader["MARITAL_STATUS"]?.ToString(),
                                WEDDING_DATE = reader["WEDDING_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["WEDDING_DATE"]) : (DateTime?)null,
                                BLOOD_GROUP = reader["BLOOD_GROUP"]?.ToString(),
                                PAN_NO = reader["PAN_NO"]?.ToString(),
                                VOTER_ID = reader["VOTER_ID"]?.ToString(),
                                AADHAR_NO = reader["AADHAR_NO"]?.ToString(),
                                BANK_CODE = reader["BANK_CODE"]?.ToString(),
                                ACT_NO = reader["ACT_NO"]?.ToString(),
                                IFSC_CODE = reader["IFSC_CODE"]?.ToString(),
                                LANGUAGE_KNOWN = reader["LANGUAGE_KNOWN"]?.ToString(),
                                HOBBIES = reader["HOBBIES"]?.ToString(),
                                GOAL_LIFE = reader["GOAL_LIFE"]?.ToString(),
                                INVOLVED_COURT = reader["INVOLVED_COURT"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new { personalDetail });
        }

        public class PersonalDetailsViewModel
        {
            public DateTime? DateOfBirth { get; set; }
            public int? Age { get; set; }
            public string MaritalStatus { get; set; }
            public DateTime? WeddingDate { get; set; }
            public string BloodGroup { get; set; }

            public string PresentAddress { get; set; }
            public int PresentCity { get; set; }
            public int PresentState { get; set; }
            public string PresentPincode { get; set; }
            public bool SameAsPresent { get; set; }
            public string PermanentAddress { get; set; }
            public int PermanentCity { get; set; }
            public int PermanentState { get; set; }
            public string PermanentPincode { get; set; }

            public string MobileNo { get; set; }
            public string EmergencyNo { get; set; }
            public string EmailId { get; set; }
            public string PassportNo { get; set; }
            public IFormFile? PassportFile { get; set; }
            public string DrivingLicenceNo { get; set; }
            public IFormFile? DrivingFile { get; set; }

            public string PANNo { get; set; }
            public IFormFile? PanFile { get; set; }
            public string VoterId { get; set; }
            public IFormFile? VoterFile { get; set; }
            public string AadharNo { get; set; }
            public IFormFile? AadharFile { get; set; }

            public string BankName { get; set; }
            public string AccountNo { get; set; }
            public string IFSCCode { get; set; }

            public string LanguagesKnown { get; set; }
            public string Hobbies { get; set; }
            public string LifeGoal { get; set; }
            public string CourtCase { get; set; }

            public string Action { get; set; }

            public int? Code { get; set; }  
        }
    }
}
