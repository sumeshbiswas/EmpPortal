using Dapper;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmshomeController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmshomeController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmshome/Index.cshtml");
        }

        public JsonResult GetddlDesignationApplied()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select code, Name From DESG_MAST where COMP_CODE={globalVar.PubCompCode} ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlCurrentLocation()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code, Name From CITY_MAST";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public async Task<IActionResult> UploadResume(IFormFile resume)
        {
            if (resume == null || resume.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }
            string extractedText;
            using (var memoryStream = new MemoryStream())
            {
                await resume.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                string extension = Path.GetExtension(resume.FileName).ToLowerInvariant();

                if (extension == ".docx")
                {
                    using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
                    extractedText = wordDoc.MainDocumentPart.Document.Body.InnerText;
                }
                else if (extension == ".pdf")
                {
                    extractedText = ExtractTextFromPdf(memoryStream);
                }
                else
                {
                    return BadRequest(new { error = "Unsupported file format", detail = $"Extension: {extension}" });
                }
            }
            var fields = ParseResumeText(extractedText);
            return Ok(fields);
        }
        private string ExtractTextFromPdf(Stream pdfStream)
        {
            var text = new StringBuilder();

            using (var document = UglyToad.PdfPig.PdfDocument.Open(pdfStream))
            {
                foreach (var page in document.GetPages())
                {
                    text.AppendLine(page.Text);
                }
            }
            return text.ToString();
        }
        private Dictionary<string, string> ParseResumeText(string text)
        {
            var fields = new Dictionary<string, string>();
            text = Regex.Replace(text, @"(\d{10})(?=[A-Za-z])", "$1 ");
            text = Regex.Replace(text, @"\s+", " ");

            // Name pattern: captures up to 3 words after 'Name' label, avoiding huge captures
            var namePattern = new Regex(@"Name\s*[:\-]?\s*(?<Name>(?:[A-Z][a-z]+(?:\s+|$)){1,3})", RegexOptions.IgnoreCase);

            // Fallback: first two capitalized words in text
            var fallbackNamePattern = new Regex(@"\b([A-Z][a-z]+)\s+([A-Z][a-z]+)\b");

            var emailPattern = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.IgnoreCase);
            var mobilePattern = new Regex(@"\b\d{10}\b");
            var companyPattern = new Regex(@"Company\s*[:\-]?\s*(?<Company>.*?)(?=\s*Designation|Date of Birth|DOB|Marital Status|Hobbies|Permanent Address|Preferred Locations|$)", RegexOptions.IgnoreCase);
            var designationPattern = new Regex(@"Designation\s*[:\-]?\s*(?<Designation>.+?)(?=Date of Birth|DOB|Marital Status|Hobbies|Permanent Address|Preferred Locations|$)", RegexOptions.IgnoreCase);

            var maritalStatusPattern = new Regex(
                @"Marital Status\s*[:\-]?\s*(?<Status>.+?)(?=Hobbies|Permanent Address|Preferred Locations|$)",
                RegexOptions.IgnoreCase);

            var locationPattern = new Regex(@"Company.*?(?<Location>Gurugram|Delhi|Noida|Bangalore|Mumbai|Pune|Chennai|Hyderabad)", RegexOptions.IgnoreCase);
            var genderPattern = new Regex(@"\b(Male|Female)\b", RegexOptions.IgnoreCase);
            var fatherPattern = new Regex(@"Father(?:'s)? Name\s*[:\-]?\s*(?<Father>[^\r\n]+)", RegexOptions.IgnoreCase);

            // Name extraction
            var nameMatch = namePattern.Match(text);
            if (nameMatch.Success)
            {
                var name = nameMatch.Groups["Name"].Value.Trim();
                // If the captured name looks like a URL or too long, fallback
                if (name.StartsWith("http", StringComparison.OrdinalIgnoreCase) || name.Split(' ').Length > 3)
                {
                    name = null;
                }
                if (!string.IsNullOrEmpty(name))
                {
                    fields["Name"] = name;
                }
            }
            ExtractCompanyAndDesignationSmart(text, fields);
            if (!fields.ContainsKey("Name"))
            {
                // Fallback: first two capitalized words, likely a name
                var fallbackMatch = fallbackNamePattern.Match(text);
                if (fallbackMatch.Success)
                {
                    fields["Name"] = $"{fallbackMatch.Groups[1].Value} {fallbackMatch.Groups[2].Value}";
                }
            }

            // Email
            var emailMatch = emailPattern.Match(text);
            if (emailMatch.Success)
                fields["Email"] = emailMatch.Value.Trim();

            // Mobile
            var mobileMatch = mobilePattern.Match(text);
            if (mobileMatch.Success)
                fields["Mobile"] = mobileMatch.Value.Trim();

            // Company
            var companyMatch = companyPattern.Match(text);
            if (companyMatch.Success)
                fields["Company"] = companyMatch.Groups["Company"].Value.Trim();

            // Designation
            var designationMatch = designationPattern.Match(text);
            if (designationMatch.Success)
                fields["Current Designation"] = designationMatch.Groups["Designation"].Value.Trim();

            var maritalMatch = maritalStatusPattern.Match(text);
            if (maritalMatch.Success)
            {
                var statusRaw = maritalMatch.Groups["Status"].Value.Trim();
                var firstWordMatch = Regex.Match(statusRaw, @"^[A-Za-z]+");
                if (firstWordMatch.Success)
                    fields["Marital Status"] = firstWordMatch.Value;
                else
                    fields["Marital Status"] = statusRaw;
            }

            // Current Location
            var locationMatch = locationPattern.Match(text);
            if (locationMatch.Success)
                fields["Current Location"] = locationMatch.Groups["Location"].Value.Trim();

            // Gender
            var genderMatch = genderPattern.Match(text);
            if (genderMatch.Success)
                fields["Gender"] = genderMatch.Value.Trim();

            // Father's Name
            var fatherMatch = fatherPattern.Match(text);
            if (fatherMatch.Success)
                fields["Father Name"] = fatherMatch.Groups["Father"].Value.Trim();

            if (fields.ContainsKey("Name"))
            {
                fields["Name"] = Regex.Replace(fields["Name"], @"(Related|Relevant|Exp.*|Industry.*).*$", "").Trim();
            }
            return fields;
        }
        private void ExtractCompanyAndDesignationSmart(string text, Dictionary<string, string> fields)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim()).ToList();

            for (int i = 0; i < lines.Count - 2; i++)
            {
                var currentLine = lines[i];
                var nextLine = lines[i + 1];
                var dateLine = lines[i + 2];

                if (Regex.IsMatch(currentLine, @"\b(Software Engineer|Developer|Programmer|Consultant|Architect|Analyst)\b", RegexOptions.IgnoreCase)
                    && Regex.IsMatch(dateLine, @"\b\d{2}/\d{4}\b|\b\d{4}\b"))
                {
                    fields["Current Designation"] ??= currentLine;
                    fields["Company"] ??= nextLine;
                    break;
                }
            }
            // If nothing is found, do partial fallback
            if (!fields.ContainsKey("Company"))
            {
                var fallback = Regex.Match(text, @"(FSL Software Technologies|Echikitsa Informatics|Space Care Solutions)", RegexOptions.IgnoreCase);
                if (fallback.Success)
                    fields["Company"] = fallback.Value;
            }
        }
        [HttpPost]
        public async Task<IActionResult> Savedata(IFormCollection form, IFormFile fileInput1, IFormFile fileInput)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string action = form["Action"];

                await using var connection = _dbConnection.GetErpConnection();
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                // === Save uploaded files to server ===
                string resumeFilePath = null; // relative path for DB
                string imageFilePath = null;  // relative path for DB

                if (fileInput1 != null && fileInput1.Length > 0)
                {
                    var resumeFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resumes");
                    if (!Directory.Exists(resumeFolder))
                        Directory.CreateDirectory(resumeFolder);

                    var resumeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileInput1.FileName)}";
                    resumeFilePath = Path.Combine("uploads", "resumes", resumeFileName).Replace("\\", "/"); // Use forward slash for URL compatibility

                    var resumeFullPath = Path.Combine(resumeFolder, resumeFileName);
                    await using var stream = new FileStream(resumeFullPath, FileMode.Create);
                    await fileInput1.CopyToAsync(stream);
                }

                if (fileInput != null && fileInput.Length > 0)
                {
                    var imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
                    if (!Directory.Exists(imageFolder))
                        Directory.CreateDirectory(imageFolder);

                    var imageFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileInput.FileName)}";
                    imageFilePath = Path.Combine("uploads", "images", imageFileName).Replace("\\", "/");

                    var imageFullPath = Path.Combine(imageFolder, imageFileName);
                    await using var stream = new FileStream(imageFullPath, FileMode.Create);
                    await fileInput.CopyToAsync(stream);
                }
                string firstName = GetValue(form, "TxtFullName")?.ToString();

                if (action == "Insert")
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@COMP_CODE", globalVar.PubCompCode);
                    parameters.Add("@TITLE", GetValue(form, "ddlTitle"));
                    parameters.Add("@FIRST_NAME", firstName);
                    parameters.Add("@FATHER_NAME", GetValue(form, "txtFatherName"));
                    parameters.Add("@POST_APPLIED", ConvertToNullableInt(form["ddlDesignation"]) ?? 0);
                    parameters.Add("@POST_APPLIED1", GetValue(form, "txtPost1Applied"));
                    parameters.Add("@CONTACT_NO", GetValue(form, "txtContact"));
                    parameters.Add("@ACTIVE", ConvertToNullableInt(form["candidateStatus"]) ?? 0);
                    parameters.Add("@ALTERNATE_NO", ConvertToNullableInt(form["txtAltContact"]) ?? 0);
                    parameters.Add("@EMAIL_ID", GetValue(form, "txtEmail"));
                    parameters.Add("@GENDER", GetValue(form, "ddlGender"));
                    parameters.Add("@MARITAL_STATUS", GetValue(form, "ddlMaritalStatus"));
                    parameters.Add("@CURR_DESG", GetValue(form, "txtCurrDesignation"));
                    parameters.Add("@CURR_ORGNIZATION", GetValue(form, "txtOrganization"));
                    parameters.Add("@NOTICE_PERIOD", ConvertToNullableInt(form["NumNotice"]) ?? 0);
                    parameters.Add("@NP_NEGO", string.IsNullOrEmpty(form["ddlNP"]) ? null : form["ddlNP"].ToString());
                    parameters.Add("@CURR_CTC", GetValue(form, "txtCTC"));
                    parameters.Add("@EXP_CTC", GetValue(form, "txtExpCTC"));
                    parameters.Add("@CURR_LOC", ConvertToNullableInt(form["ddlLocation"]) ?? 0);
                    parameters.Add("@PREF_LOC", ConvertToNullableInt(form["ddlPrefLocation"]) ?? 0);
                    parameters.Add("@SALARY_NEGO", GetValue(form, "ddlSalary"));
                    parameters.Add("@REFERENCE_NAME", GetValue(form, "ddlReferredBy"));
                    parameters.Add("@Unit", GetValue(form, "ddlUnits"));
                    parameters.Add("@REFERENCE_NAME1", GetValue(form, "txtRefBy"));

                    parameters.Add("@REFERENCE_NO", GetValue(form, "txtContactRefBy"));
                    parameters.Add("@REMARKS", GetValue(form, "txtRemarks"));
                    parameters.Add("@REMARKS2", GetValue(form, "txtOthRemarks"));
                    parameters.Add("@APPLICATION_DATE", DateTime.Now);
                    parameters.Add("@FAPROV_STATUS", GetValue(form, "candidateStatus"));
                    parameters.Add("@MRN", GetValue(form, "ddlMRNNo"));

                    // Save relative file path instead of just file name
                    parameters.Add("@FILE_NAME", resumeFilePath);

                    // Optionally add image file path parameter if your DB supports it
                    //parameters.Add("@IMAGE_FILE_NAME", imageFilePath);

                    parameters.Add("@UUSER", globalVar.PubUserId);
                    parameters.Add("@UDATE", DateTime.Now);
                    parameters.Add("@EUSER", "");
                    parameters.Add("@EDATE", "");
                    parameters.Add("@AED", "A");
                    parameters.Add("@WSID", globalVar.PubWorkStationID);
                    parameters.Add("@LIP", globalVar.PubLocalId);
                    parameters.Add("@LID", Environment.MachineName);
                    parameters.Add("@Action", "Insert");

                    await connection.ExecuteAsync("usp_Insert_HRMS_EMPBASIC", parameters, transaction, commandType: CommandType.StoredProcedure);

                    // Retrieve new CODE
                    string email = GetValue(form, "txtEmail")?.ToString();
                    string name = GetValue(form, "TxtFullName")?.ToString();

                    var newCode = await connection.ExecuteScalarAsync<int>(
                        @"SELECT TOP 1 CODE FROM HRMS_EMPBASIC 
                  WHERE EMAIL_ID = @Email AND FIRST_NAME = @Name AND COMP_CODE = @CompCode
                  ORDER BY CODE DESC",
                        new { Email = email, Name = name, CompCode = globalVar.PubCompCode },
                        transaction: transaction
                    );

                    // Insert into other related tables
                    var insertParams = new DynamicParameters();
                    insertParams.Add("@COMP_CODE", globalVar.PubCompCode);
                    insertParams.Add("@CODE", newCode);
                    insertParams.Add("@YEAR_CODE", globalVar.PubFYearCode);

                    var tableInsertQueries = new[]
                    {
                        "INSERT INTO HRMS_EMPPERSONAL (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPFAMILY (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPEDUCATION (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPWORK (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPREFERENCE (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_Interview (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        //"INSERT INTO PAY_EMP_INTENT (COMP_CODE, V_NO, YEAR_CODE, BRANCH_CODE) VALUES (@COMP_CODE, @CODE, @YEAR_CODE, 1)"

                    };

                    foreach (var query in tableInsertQueries)
                    {
                        await connection.ExecuteAsync(query, insertParams, transaction);
                    }

                    //await MarkStepCompleteInternal(newCode);
                    HttpContext.Session.SetInt32("CandidateCode", newCode);
                    HttpContext.Session.SetString("FIRST_NAME", firstName);

                    await transaction.CommitAsync();
                    return Ok(new { message = "Form submitted successfully.", Code = newCode });
                }
                else if (action == "Update")
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@COMP_CODE", globalVar.PubCompCode);
                    parameters.Add("@Code", GetValue(form, "Code"));
                    parameters.Add("@TITLE", GetValue(form, "ddlTitle"));
                    parameters.Add("@FIRST_NAME", firstName);
                    parameters.Add("@FATHER_NAME", GetValue(form, "txtFatherName"));
                    parameters.Add("@POST_APPLIED", ConvertToNullableInt(form["ddlDesignation"]) ?? 0);
                    parameters.Add("@POST_APPLIED1", GetValue(form, "txtPost1Applied"));
                    parameters.Add("@CONTACT_NO", GetValue(form, "txtContact"));
                    parameters.Add("@ACTIVE", ConvertToNullableInt(form["candidateStatus"]) ?? 0);
                    parameters.Add("@ALTERNATE_NO", ConvertToNullableInt(form["txtAltContact"]) ?? 0);
                    parameters.Add("@EMAIL_ID", GetValue(form, "txtEmail"));
                    parameters.Add("@GENDER", GetValue(form, "ddlGender"));
                    parameters.Add("@MARITAL_STATUS", GetValue(form, "ddlMaritalStatus"));
                    parameters.Add("@CURR_DESG", GetValue(form, "txtCurrDesignation"));
                    parameters.Add("@CURR_ORGNIZATION", GetValue(form, "txtOrganization"));
                    parameters.Add("@NOTICE_PERIOD", ConvertToNullableInt(form["NumNotice"]) ?? 0);
                    parameters.Add("@NP_NEGO", string.IsNullOrEmpty(form["ddlNP"]) ? null : form["ddlNP"].ToString());
                    parameters.Add("@CURR_CTC", GetValue(form, "txtCTC"));
                    parameters.Add("@EXP_CTC", GetValue(form, "txtExpCTC"));
                    parameters.Add("@CURR_LOC", ConvertToNullableInt(form["ddlLocation"]) ?? 0);
                    parameters.Add("@PREF_LOC", ConvertToNullableInt(form["ddlPrefLocation"]) ?? 0);
                    parameters.Add("@SALARY_NEGO", GetValue(form, "ddlSalary"));
                    parameters.Add("@REFERENCE_NAME", GetValue(form, "txtRefBy"));
                    parameters.Add("@REFERENCE_NO", GetValue(form, "txtContactRefBy"));
                    parameters.Add("@REMARKS", GetValue(form, "txtRemarks"));
                    parameters.Add("@REMARKS2", GetValue(form, "txtOthRemarks"));
                    parameters.Add("@APPLICATION_DATE", DateTime.Now);
                    parameters.Add("@FAPROV_STATUS", GetValue(form, "candidateStatus"));
                    parameters.Add("@Unit", GetValue(form, "ddlUnits"));
                    parameters.Add("@REFERENCE_NAME1", GetValue(form, "txtRefBy"));
                    parameters.Add("@MRN", GetValue(form, "ddlMRNNo"));

                    // Use new file paths if files uploaded, otherwise null or keep existing?
                    parameters.Add("@FILE_NAME", resumeFilePath);

                    // Optionally add image path for update
                    parameters.Add("@UUSER", globalVar.PubUserId);
                    parameters.Add("@UDATE", DateTime.Now);
                    parameters.Add("@EUSER", globalVar.PubUserId); // Corrected
                    parameters.Add("@EDATE", DateTime.Now);
                    parameters.Add("@AED", "E");  // E for Edit (Update)
                    parameters.Add("@WSID", globalVar.PubWorkStationID);
                    parameters.Add("@LIP", globalVar.PubLocalId);
                    parameters.Add("@LID", Environment.MachineName);
                    parameters.Add("@Action", "Update");

                    // Call your Update stored procedure here
                    await connection.ExecuteAsync("usp_Insert_HRMS_EMPBASIC", parameters, transaction, commandType: CommandType.StoredProcedure);

                    await transaction.CommitAsync();
                    return Ok(new { message = "Form updated successfully." });
                }
                else
                {
                    return BadRequest(new { error = "Invalid action specified." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        private string? GetValue(IFormCollection form, string key)
        {
            var val = form[key];
            return string.IsNullOrWhiteSpace(val) ? null : val.ToString();
        }
        private int? ConvertToNullableInt(string input)
        {
            return int.TryParse(input, out int result) ? result : (int?)null;
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
        public IActionResult GetCandidateBasicDetailList(int code)
        {
            var candidateDetail = new List<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("usp_Insert_HRMS_EMPBASIC", con))
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
                            candidateDetail.Add(new
                            {
                                M_TYPE = reader["M_TYPE"]?.ToString(),
                                TITLE = reader["TITLE"]?.ToString(),
                                FIRST_NAME = reader["FIRST_NAME"]?.ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                FATHER_NAME = reader["FATHER_NAME"]?.ToString(),
                                POST_APPLIED = reader["POST_APPLIED"]?.ToString(),
                                REFERENCE_NAME = reader["REFERENCE_NAME"]?.ToString(),
                                REFERENCE_NO = reader["REFERENCE_NO"]?.ToString(),
                                APPLICATION_DATE = reader["APPLICATION_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["APPLICATION_DATE"]) : (DateTime?)null,
                                FAPROV_STATUS = reader["FAPROV_STATUS"]?.ToString(),
                                FILE_NAME = reader["FILE_NAME"]?.ToString(),
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToBoolean(reader["ACTIVE"]) : false,
                                CONTACT_NO = reader["CONTACT_NO"]?.ToString(),
                                ALTERNATE_NO = reader["ALTERNATE_NO"]?.ToString(),
                                EMAIL_ID = reader["EMAIL_ID"]?.ToString(),
                                CURR_DESG = reader["CURR_DESG"]?.ToString(),
                                CURR_ORGNIZATION = reader["CURR_ORGNIZATION"]?.ToString(),
                                CURR_CTC = reader["CURR_CTC"]?.ToString(),
                                EXP_CTC = reader["EXP_CTC"]?.ToString(),
                                CURR_LOC = reader["CURR_LOC"]?.ToString(),
                                PREF_LOC = reader["PREF_LOC"]?.ToString(),
                                NOTICE_PERIOD = reader["NOTICE_PERIOD"]?.ToString(),
                                SALARY_NEGO = reader["SALARY_NEGO"]?.ToString(),
                                REMARKS = reader["REMARKS"]?.ToString(),
                                REMARKS2 = reader["REMARKS2"]?.ToString(),
                                GENDER = reader["GENDER"]?.ToString(),
                                MARITAL_STATUS = reader["MARITAL_STATUS"]?.ToString(),
                                POST_APPLIED1 = reader["POST_APPLIED1"]?.ToString(),
                                Unit = reader["Unit"]?.ToString(),
                                REFERENCE_NAME1 = reader["REFERENCE_NAME1"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { candidateDetail });
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
            string stepName = "hrmshome";
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
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "template", "template.xlsx");

            if (!System.IO.File.Exists(filePath))
                return NotFound("Template file not found.");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "template.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile fileInput2)
        {
            if (fileInput2 == null || fileInput2.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = new MemoryStream();
            await fileInput2.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            var gv = _globalVariableService.GetGlobalVariables();

            if (worksheet == null)
                return BadRequest("No worksheet found in the Excel file.");

            int rowCount = worksheet.Dimension.Rows;
            using SqlConnection conn = _dbConnection.GetErpConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction(); 

            try
            {
                // Get current max CODE once
                int currentMaxEmpCode = 0;
                using (SqlCommand maxCodeCmd = new SqlCommand("SELECT ISNULL(MAX(CODE), 0) FROM HRMS_EMPBASIC", conn, transaction))
                {
                    object result = await maxCodeCmd.ExecuteScalarAsync();
                    if (result != null && int.TryParse(result.ToString(), out int maxCode))
                    {
                        currentMaxEmpCode = maxCode;
                    }
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    string firstName = worksheet.Cells[row, 1].Text?.Trim();
                    string name = worksheet.Cells[row, 2].Text?.Trim();
                    string postApplied = worksheet.Cells[row, 3].Text?.Trim();
                    string referenceName = worksheet.Cells[row, 4].Text?.Trim();
                    string applicationDate = worksheet.Cells[row, 5].Text?.Trim();
                    string faProvRemarks = worksheet.Cells[row, 6].Text?.Trim();
                    string contactNo = worksheet.Cells[row, 7].Text?.Trim();
                    string alternateNo = worksheet.Cells[row, 8].Text?.Trim();
                    string emailId = worksheet.Cells[row, 9].Text?.Trim();
                    string currDesg = worksheet.Cells[row, 10].Text?.Trim();
                    string currOrganization = worksheet.Cells[row, 11].Text?.Trim();
                    string currCtc = worksheet.Cells[row, 12].Text?.Trim();
                    string expCtc = worksheet.Cells[row, 13].Text?.Trim();
                    string currLoc = worksheet.Cells[row, 14].Text?.Trim();
                    string prefLoc = worksheet.Cells[row, 15].Text?.Trim();
                    string noticePeriod = worksheet.Cells[row, 16].Text?.Trim();
                    string npNego = worksheet.Cells[row, 17].Text?.Trim();
                    string gender = worksheet.Cells[row, 18].Text?.Trim();
                    string maritalStatus = worksheet.Cells[row, 19].Text?.Trim();
                    string unit = worksheet.Cells[row, 20].Text?.Trim();
                    string Referredby_Name= worksheet.Cells[row, 21].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(firstName) &&
                        string.IsNullOrWhiteSpace(name) &&
                        string.IsNullOrWhiteSpace(postApplied))
                    {
                        continue;
                    }
                    currentMaxEmpCode++;
                    int newCode = currentMaxEmpCode;
                    int? genderValue = gender?.ToLower() switch
                    {
                        "male" => 1,
                        "female" => 2,
                        _ => 3
                    };
                    int? maritalStatusValue = maritalStatus?.ToLower() switch
                    {
                        "single" => 1,
                        "married" => 2,
                        _ => 3
                    };
                    string query = @"INSERT INTO HRMS_EMPBASIC 
                        (CODE, COMP_CODE, FIRST_NAME, NAME, POST_APPLIED1, REFERENCE_NAME, APPLICATION_DATE, FAPROV_REMARKS,
                        CONTACT_NO, ALTERNATE_NO, EMAIL_ID, CURR_DESG, CURR_ORGNIZATION, CURR_CTC,
                        EXP_CTC, CURR_LOC, PREF_LOC, NOTICE_PERIOD, NP_NEGO, GENDER, MARITAL_STATUS,Unit,REFERENCE_NAME1)
                        VALUES
                        (@CODE, @COMP_CODE, @FIRST_NAME, @NAME, @POST_APPLIED1, @REFERENCE_NAME, @APPLICATION_DATE, @FAPROV_REMARKS,
                        @CONTACT_NO, @ALTERNATE_NO, @EMAIL_ID, @CURR_DESG, @CURR_ORGNIZATION, @CURR_CTC,
                        @EXP_CTC, @CURR_LOC, @PREF_LOC, @NOTICE_PERIOD, @NP_NEGO, @GENDER, @MARITAL_STATUS,@Unit,@REFERENCE_NAME1)";

                    using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CODE", newCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@FIRST_NAME", (object)name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAME", (object)name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@POST_APPLIED1", (object)postApplied ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REFERENCE_NAME", (object)referenceName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REFERENCE_NAME1", (object)Referredby_Name ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@APPLICATION_DATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", (object)faProvRemarks ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CONTACT_NO", (object)contactNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ALTERNATE_NO", (object)alternateNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMAIL_ID", (object)emailId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CURR_DESG", (object)currDesg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CURR_ORGNIZATION", (object)currOrganization ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CURR_CTC", (object)ToNullableDecimal(currCtc) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EXP_CTC", (object)ToNullableDecimal(expCtc) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CURR_LOC", (object)ToNullableInt(currLoc) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PREF_LOC", (object)ToNullableInt(prefLoc) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOTICE_PERIOD", (object)ToNullableInt(noticePeriod) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NP_NEGO", (object)npNego ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GENDER", genderValue ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MARITAL_STATUS", maritalStatusValue ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Unit", (object)unit ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Insert into related tables using Dapper and transaction
                    var insertParams = new DynamicParameters();
                    insertParams.Add("@COMP_CODE", gv.PubCompCode);
                    insertParams.Add("@CODE", newCode);

                    var tableInsertQueries = new[]
                    {
                        "INSERT INTO HRMS_EMPPERSONAL (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPFAMILY (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPEDUCATION (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPWORK (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_EMPREFERENCE (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)",
                        "INSERT INTO HRMS_Interview (COMP_CODE, CODE) VALUES (@COMP_CODE, @CODE)"
                    };
                    foreach (var insertQuery in tableInsertQueries)
                    {
                        await conn.ExecuteAsync(insertQuery, insertParams, transaction);
                    }
                    await MarkStepCompleteInternal(newCode);
                    HttpContext.Session.SetInt32("CandidateCode", newCode);
                }
                // Commit transaction
                transaction.Commit();
                return Ok("Excel data uploaded successfully.");
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                transaction.Rollback();
                return StatusCode(500, $"Error during Excel upload: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult Checkhrmstable(string code)
        {
            var result = new
            {
                basic = 0,
                personal = 0,
                education = 0,
                family = 0,
                reference = 0,
                work = 0,
                interview = 0,
                letterintent = 0
            };
            var gv = _globalVariableService.GetGlobalVariables();
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
                            };
                        }
                    }
                }
            }
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetddlMRN()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var dataList = await _dbHelper.GetJsonDataAsync($@" SELECT Code, Code AS Name FROM PAY_NEWEMPREQ WHERE COMP_CODE = '{gv.PubCompCode}'");
            return Json(new { status = true, data = dataList });
        }

        [HttpGet]
        public async Task<IActionResult> GetMRNDetails(int mrnNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = await _dbHelper.GetJsonDataAsync($@" SELECT DEPT_CODE, DESG_CODE FROM PAY_NEWEMPREQ WHERE CODE = '{mrnNo}' AND COMP_CODE = '{gv.PubCompCode}'");
            return Json(new { status = true, data = data });
        }
        private int? ToNullableInt(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Trim().ToUpper() == "NULL")
                return null;

            if (int.TryParse(input, out var result))
                return result;

            return null; // or throw/log if needed
        }
        private decimal? ToNullableDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Trim().ToUpper() == "NULL") return null;

            if (decimal.TryParse(input, out var result)) return result;
            return null;
        }
        private DateTime? ToNullableDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Trim().ToUpper() == "NULL")
                return null;

            if (DateTime.TryParse(input, out var result))
                return result;

            return null;
        }

    }
}
