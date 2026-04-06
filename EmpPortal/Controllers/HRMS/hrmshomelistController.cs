using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using Spire.Doc;



namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmshomelistController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public hrmshomelistController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/hrmshomelist/Index.cshtml");
        }
        [HttpGet]
        public JsonResult Gethrmshomelist(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("usp_Insert_HRMS_EMPBASIC", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        // Read paginated data rows
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                SearchCode = reader["SearchCode"]?.ToString() ?? "",
                                FIRST_NAME = reader["FIRST_NAME"]?.ToString() ?? "",
                                POST_APPLIED1 = reader["POST_APPLIED1"]?.ToString() ?? "",
                                Unit = reader["Unit"]?.ToString() ?? "",
                                CURR_CTC = reader["CURR_CTC"]?.ToString() ?? "",
                                EXP_CTC = reader["EXP_CTC"]?.ToString() ?? "",
                                CURR_LOC = reader["CURR_LOC"]?.ToString() ?? "",
                                EMAIL_ID = reader["EMAIL_ID"]?.ToString() ?? "",
                                CONTACT_NO = reader["CONTACT_NO"]?.ToString() ?? "",
                                ACTIVE = reader["ACTIVE"]?.ToString() ?? ""
                            });
                        }
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader.GetInt32(0);
                        }
                    }
                }
            }
            return Json(new { items = results, totalCount });
        }

        [HttpGet]
        public IActionResult DownloadResume(string searchCode)
        {
            if (string.IsNullOrEmpty(searchCode))
                return BadRequest("Invalid resume identifier");

            string fileNameFromDb = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT FILE_NAME FROM HRMS_EMPBASIC WHERE code=@code", con))
                {
                    cmd.Parameters.AddWithValue("@code", searchCode);
                    fileNameFromDb = cmd.ExecuteScalar() as string;
                }
            }
            if (string.IsNullOrEmpty(fileNameFromDb))
                return NotFound("Resume not found");
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(uploadsFolder, fileNameFromDb.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server");
            string contentType = "application/octet-stream";
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".pdf": contentType = "application/pdf"; break;
                case ".doc": contentType = "application/msword"; break;
                case ".docx": contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; break;
                case ".xls": contentType = "application/vnd.ms-excel"; break;
                case ".xlsx": contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; break;
            }
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var downloadFileName = Path.GetFileName(filePath);
            return File(fileBytes, contentType, downloadFileName);
        }
        [HttpPost]
        public JsonResult UpdateActiveStatus(string code, string status)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE HRMS_EMPBASIC SET ACTIVE=@ACTIVE WHERE CODE=@CODE AND COMP_CODE=@COMP_CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@ACTIVE", status);
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        con.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                            return Json(new { success = true });
                        else
                            return Json(new { success = false });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PreviewResume(string searchCode)
        {
            if (string.IsNullOrEmpty(searchCode))
                return BadRequest("Invalid code");

            string fileName = "";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT FILE_NAME FROM HRMS_EMPBASIC WHERE CODE=@code", con))
                {
                    cmd.Parameters.AddWithValue("@code", searchCode);
                    fileName = cmd.ExecuteScalar()?.ToString();
                }
            }

            if (string.IsNullOrEmpty(fileName))
                return NotFound("File not found in DB");

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, fileName.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found on server");

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();

            // 🔥 If DOC / DOCX → Convert to PDF
            if (ext == ".doc" || ext == ".docx")
            {
                fullPath = ConvertToPdf(fullPath);
            }

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, "application/pdf");
        }

        private string ConvertToPdf(string docPath)
        {
            string pdfPath = Path.ChangeExtension(docPath, ".pdf");
        
            // Agar PDF already exist karta hai to dubara convert nahi karega
            if (!System.IO.File.Exists(pdfPath))
            {
                Document document = new Document();
                document.LoadFromFile(docPath);
                document.SaveToFile(pdfPath, FileFormat.PDF);
            }
        
            return pdfPath;
        }



    }
}
