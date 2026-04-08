using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class EmpLoginController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly string PubWhatsupTokenId ="0ba3f59a551b9a8881caba3572031b81183859298391c1cbdc8e915ec725430a";

        public EmpLoginController( DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/EmpLogin/Index.cshtml");
        }


        [HttpGet]
        public JsonResult GetddlCompany()
        {
            List<object> list = new();

            using SqlConnection con = _dbConnection.GetConDbConnection();
            using SqlCommand cmd = new("SELECT CODE, NAME FROM COMP_MAST", con);

            con.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new
                {
                    value = Convert.ToInt32(dr["CODE"]),
                    text = dr["NAME"].ToString()
                });
            }

            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SendOTP(string mobile, int compCode)
        {
            HttpContext.Session.SetString("COMP_CODE", compCode.ToString());
            if (string.IsNullOrWhiteSpace(mobile))
                return Json(new { success = false, message = "Mobile required" });

            string otp = new Random().Next(100000, 999999).ToString();

            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();

            SqlCommand cmd = new SqlCommand(@"
            IF EXISTS ( SELECT 1 FROM EmpPortalLogin WHERE MOBILE_NO = @Mobile AND COMP_CODE = @CompCode AND OTP_EXPIRY > GETDATE())
            BEGIN
                SELECT -1
            END
            ELSE
            BEGIN
                IF EXISTS ( SELECT 1  FROM EmpPortalLogin WHERE MOBILE_NO = @Mobile  AND COMP_CODE = @CompCode)
                BEGIN
                    UPDATE EmpPortalLogin SET OTP_CODE = @Otp, OTP_EXPIRY = DATEADD(MINUTE, 10, GETDATE()), ACTIVE = 1, UDATE = GETDATE() WHERE MOBILE_NO = @Mobile AND COMP_CODE = @CompCode
                END
                ELSE
                BEGIN
                    INSERT INTO EmpPortalLogin(COMP_CODE,MOBILE_NO,OTP_CODE, OTP_EXPIRY, ACTIVE, UDATE)
                    VALUES(@CompCode,@Mobile, @Otp, DATEADD(MINUTE, 10, GETDATE()), 1, GETDATE())
                END
            END", con);

            cmd.Parameters.AddWithValue("@Mobile", mobile);
            cmd.Parameters.AddWithValue("@CompCode", compCode);
            cmd.Parameters.AddWithValue("@Otp", otp);

            var result = cmd.ExecuteScalar();
            if (result != null && result.ToString() == "-1")
                return Json(new { success = false, message = "OTP already sent. Wait 10 minutes." });

            bool sent = await SendWhatsAppMessage("vno", mobile, otp);
            if (!sent)
                return Json(new { success = false, message = "WhatsApp OTP failed" });

            return Json(new { success = true, message = "OTP sent successfully" });
        }
        private async Task<bool> SendWhatsAppMessage(string templateName, string phoneNumber, string f1 = "", string f2 = "",
        string f3 = "", string f4 = "", string f5 = "", string f6 = "", string f7 = "", string f8 = "", string f9 = "", string f10 = "")
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string apiUrl = "https://sparklebot.in/api/v1/pashupatigrpcom/messages/template";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =  new AuthenticationHeaderValue("Bearer", PubWhatsupTokenId);
                client.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue("application/json"));

                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(templateName), "template_name");
                form.Add(new StringContent("en"), "template_language");
                form.Add(new StringContent(phoneNumber), "phone_number");

                if (templateName == "plastindia" || templateName == "vno")
                {
                    if (string.IsNullOrEmpty(f1))
                        f1 = "NA";
                    form.Add(new StringContent(f1), "field_1");
                }
                var response = await client.PostAsync(apiUrl, form);
                var responseText = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode && responseText.ToLower().Contains("success");
            }
            catch
            {
                return false;
            }
        }
        [HttpPost]
        public IActionResult VerifyOTP(string mobile, int compCode, string otp, string pin)
        {
            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();

            using SqlCommand cmdCount = new SqlCommand(@"  SELECT COUNT(MOBILE_NO)  FROM EmpPortalLogin WHERE MOBILE_NO = @Mobile AND COMP_CODE = @CompCode", con);
            cmdCount.Parameters.AddWithValue("@Mobile", mobile);
            cmdCount.Parameters.AddWithValue("@CompCode", compCode);

            int count = (int)cmdCount.ExecuteScalar();

            if (count == 1)
            {
                return Json(new { success = false, message = "User already exists" });
            }

            SqlCommand cmd = new SqlCommand(@"SELECT OTP_CODE, OTP_EXPIRY FROM EmpPortalLogin WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);

            cmd.Parameters.AddWithValue("@Mobile", mobile);
            cmd.Parameters.AddWithValue("@CompCode", compCode);

            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
                return Json(new { success = false, message = "User not found" });

            if (DateTime.Now > Convert.ToDateTime(dr["OTP_EXPIRY"]))
                return Json(new { success = false, message = "OTP expired" });

            if (otp != dr["OTP_CODE"].ToString())
                return Json(new { success = false, message = "Invalid OTP" });

            dr.Close();

            string hashedPin = BCrypt.Net.BCrypt.HashPassword(pin);

            SqlCommand up = new SqlCommand(@"UPDATE EmpPortalLogin SET PIN=@Pin, ACTIVE=1, OTP_CODE=NULL, OTP_EXPIRY=NULL
            WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);
            up.Parameters.AddWithValue("@Pin", hashedPin);
            up.Parameters.AddWithValue("@Mobile", mobile);
            up.Parameters.AddWithValue("@CompCode", compCode);
            up.ExecuteNonQuery();
            return Json(new { success = true, message = "Registration successful" });
        }

        //[HttpPost]
        //public IActionResult EmployeeLogin(string mobile, string pin, int compCode) 
        //{
        //    //HttpContext.Session.Clear();
        //    HttpContext.Session.Clear();
        //    //Response.Cookies.Delete(".TravelExpense.Session");
        //    //Response.Cookies.Delete(".AspNetCore.Session");
        //    HttpContext.Session.SetString("COMP_CODE", compCode.ToString());

        //    using SqlConnection con = _dbConnection.GetErpConnection();
        //    con.Open();

        //    SqlCommand cmd = new SqlCommand(@"SELECT PIN, ACTIVE FROM EmpPortalLogin WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);

        //    cmd.Parameters.AddWithValue("@Mobile", mobile);
        //    cmd.Parameters.AddWithValue("@CompCode", compCode);

        //    using SqlDataReader dr = cmd.ExecuteReader();

        //    if (!dr.HasRows)  // Check if any rows were returned
        //    {
        //        return Json(new { success = false, message = "Invalid credentials" });
        //    }


        //    if (Convert.ToInt32(dr["ACTIVE"]) != 1)
        //    {
        //        return Json(new { success = false, message = "Please complete registration first" });
        //    }


        //    bool validPin = BCrypt.Net.BCrypt.Verify(pin, dr["PIN"].ToString());

        //    if (!validPin)
        //    {
        //        return Json(new { success = false, message = "Invalid PIN" });
        //    }


        //    HttpContext.Session.SetString("MOBILE", mobile);
        //    HttpContext.Session.SetString("COMP_CODE", compCode.ToString());

        //    return Json(new
        //    {
        //        success = true,
        //        redirectUrl = Url.Action("Index", "EmployeeDashboard")
        //    });
        //}


        [HttpPost]
        public IActionResult EmployeeLogin(string mobile, string pin, int compCode , string COMBINEOTP)
        {
            // Clear any existing session
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("COMP_CODE", compCode.ToString());

            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();


                SqlCommand cmd1 = new SqlCommand(@"
                SELECT OTP_CODE, OTP_EXPIRY 
                FROM EmpPortalLogin 
                WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);

                cmd1.Parameters.AddWithValue("@Mobile", mobile);
                cmd1.Parameters.AddWithValue("@CompCode", compCode);

                using SqlDataReader dr = cmd1.ExecuteReader();


            if (!dr.Read())
            {
                return Json(new { success = false, message = "Mobile not found or OTP not generated" });
            }

            if (COMBINEOTP != dr["OTP_CODE"].ToString())
            {
                return Json(new { success = false, message = "Invalid OTP" });
            }        

            dr.Close();


            // Prepare the SQL query
            string query = @"SELECT PIN, ACTIVE FROM EmpPortalLogin 
                     WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Mobile", mobile);
            cmd.Parameters.AddWithValue("@CompCode", compCode);

            // Use SqlDataAdapter to fill a DataTable
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }

            // Check if any record exists
            if (dt.Rows.Count == 0)
            {
                return Json(new { success = false, message = "Invalid credentials" });
            }

            DataRow row = dt.Rows[0];

            // Check if user is active
            if (Convert.ToInt32(row["ACTIVE"]) != 1)
            {
                return Json(new { success = false, message = "Please complete registration first" });
            }

            // Verify PIN
            bool validPin = BCrypt.Net.BCrypt.Verify(pin, row["PIN"].ToString());
            if (!validPin)
            {
                return Json(new { success = false, message = "Invalid PIN" });
            }




            string empQuery = @"SELECT EMP_ID, COMP_CODE 
                        FROM EMP_MAST 
                        WHERE MOBILE=@Mobile AND COMP_CODE=@CompCode";

            using SqlCommand empCmd = new SqlCommand(empQuery, con);
            empCmd.Parameters.AddWithValue("@Mobile", mobile);
            empCmd.Parameters.AddWithValue("@CompCode", compCode);

            using SqlDataReader empDr = empCmd.ExecuteReader();

            if (!empDr.Read())
            {
                return Json(new { success = false, message = "Employee not found" });
            }

            int empId = empDr["EMP_ID"] != DBNull.Value
             ? Convert.ToInt32(empDr["EMP_ID"])
             : 0;



            // Set session variables
            HttpContext.Session.SetString("MOBILE", mobile);
            HttpContext.Session.SetString("COMP_CODE", compCode.ToString());
            HttpContext.Session.SetInt32("EMP_ID", empId);






            // Return success
            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("Index", "EmployeeDashboard")
            });
        }



        [HttpPost]
        public IActionResult CheckPassword(string mobile, string pin, int compCode)
        {
            /* SET SESSION FIRST */
            HttpContext.Session.SetString("COMP_CODE", compCode.ToString());

            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();

            SqlCommand cmd = new SqlCommand(@"
        SELECT PIN, ACTIVE
        FROM EmpPortalLogin
        WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);

            cmd.Parameters.AddWithValue("@Mobile", mobile);
            cmd.Parameters.AddWithValue("@CompCode", compCode);

            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
                return Json(new { success = false, message = "User not found" });

            if (Convert.ToInt32(dr["ACTIVE"]) != 1)
                return Json(new { success = false, message = "Please complete registration first" });

            bool validPin = BCrypt.Net.BCrypt.Verify(pin, dr["PIN"].ToString());

            if (!validPin)
                return Json(new { success = false, message = "Please enter correct password" });

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult updatePin(string mobile, int compCode,  string pin , string otp)
        {
            HttpContext.Session.SetString("COMP_CODE", compCode.ToString());
            using SqlConnection con = _dbConnection.GetErpConnection();

            con.Open();

            SqlCommand cmd = new SqlCommand(@"SELECT OTP_CODE, OTP_EXPIRY FROM EmpPortalLogin WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);

            cmd.Parameters.AddWithValue("@Mobile", mobile);
            cmd.Parameters.AddWithValue("@CompCode", compCode);

            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
                return Json(new { success = false, message = "User not found" });

            if (DateTime.Now > Convert.ToDateTime(dr["OTP_EXPIRY"]))
                return Json(new { success = false, message = "OTP expired" });

            if (otp != dr["OTP_CODE"].ToString())
                return Json(new { success = false, message = "Invalid OTP" });

            dr.Close();

            string hashedPin = BCrypt.Net.BCrypt.HashPassword(pin);

            SqlCommand up = new SqlCommand(@"UPDATE EmpPortalLogin SET PIN=@Pin, ACTIVE=1, OTP_CODE=NULL, OTP_EXPIRY=NULL
            WHERE MOBILE_NO=@Mobile AND COMP_CODE=@CompCode", con);

            up.Parameters.AddWithValue("@Pin", hashedPin);
            up.Parameters.AddWithValue("@Mobile", mobile);
            up.Parameters.AddWithValue("@CompCode", compCode);
            up.ExecuteNonQuery();

            return Json(new { success = true, message = "Password change SuccessFully" });
        }

        //Code Commented by sumesh
        //public IActionResult Logout()
        //{
        //    // Clear all session data
        //    HttpContext.Session.Clear();
        //    Response.Cookies.Delete(".TravelExpense.Session");
        //    // Redirect to login page (or a home page, depending on your requirements)
        //    return RedirectToAction("Index", "EmpLogin");
        //}

        //new code
        [HttpPost]
        public IActionResult Logout()
        {
            // ✅ Clear session data
            HttpContext.Session.Clear();

            // ✅ Abandon session completely (important for live server)
            HttpContext.Session.Remove("MOBILE");
            HttpContext.Session.Remove("COMP_CODE");
            HttpContext.Session.Remove("EMP_ID");

            return Json(new { success = true });
        }


    }
}
