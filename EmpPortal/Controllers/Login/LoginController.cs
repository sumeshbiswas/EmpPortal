using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Text;
using EmpPortal.Helpers;
using EmpPortal.Models.Login;
using travelexpensemanagement.Dbconnection;

namespace EmpPortal.Controllers
{
    public class LoginController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly EncryptionHelper _encryptionHelper;
        public LoginController(DataBaseConnection dbConnection, EncryptionHelper encryptionHelper)
        {
            _dbConnection = dbConnection;
            _encryptionHelper = encryptionHelper;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [EnableRateLimiting("LoginLimiter")]
        public IActionResult GetAction(LoginViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserMasterCode) || string.IsNullOrEmpty(model.Password))
            {
                TempData["ErrorMessage"] = "Please enter username and password.";
                return RedirectToAction(nameof(Index));
            }
           
            try
            {
                string pcName = Environment.MachineName;
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                string localIp = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

                using (SqlConnection con = _dbConnection.GetConDbConnection())
                {
                    if (con.State != ConnectionState.Open)
                    {
                        con.Open(); 
                    }
                    string subUserQuery = "SELECT 1 FROM SUBUSER_MAST WHERE COMP_CODE = @CompanyCode AND USER_CODE = @UserMasterCode";
                    using (SqlCommand cmd = new SqlCommand(subUserQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyCode", model.CompanyCode);
                        cmd.Parameters.AddWithValue("@UserMasterCode", model.UserMasterCode);

                        var result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            TempData["ErrorMessage"] = "User not registered in the selected company.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                }
                // Step 2: Check credentials, IP/PC, and IsLoggedIn
                using (SqlConnection con = _dbConnection.GetConDbConnection())
                {
                    if (con.State != ConnectionState.Open)
                    {
                        con.Open();
                    }
                    //string userQuery = "SELECT CODE, WebPASSWD, PASSWD, Active, Lip, PC_NAME, PC_NAME2, PC_NAME3, COMP_CODE, USER_NAME, IsLoggedIn,USER_LEVEL FROM USER_MAST WHERE COMP_CODE = @CompanyCode AND CODE = @UserMasterCode";
                    string userQuery = "SELECT CODE, WebPASSWD, PASSWD, Active, Lip, PC_NAME, PC_NAME2, PC_NAME3, USER_NAME, IsLoggedIn,USER_LEVEL FROM USER_MAST WHERE CODE = @UserMasterCode";
                    using (SqlCommand cmd = new SqlCommand(userQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyCode", model.CompanyCode);
                        cmd.Parameters.AddWithValue("@UserMasterCode", model.UserMasterCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int isActive = reader["Active"] != DBNull.Value ? Convert.ToInt32(reader["Active"]) : 0;
                                if (isActive != 1)
                                {
                                    TempData["ErrorMessage"] = "User is inactive. Please contact administrator.";
                                    return RedirectToAction(nameof(Index));
                                }

                                string storedEncryptedPassword = reader["WebPASSWD"].ToString();
                                //string storedEncryptedOldPassword = reader["PASSWD"].ToString();
                                //string decryptedOldPassword = Decrypt(model.Password);
                                //string decryptedPassword = EncryptionHelper.Decrypt(storedEncryptedPassword);
                                string decryptedPassword = _encryptionHelper.Decrypt(storedEncryptedPassword);

                                if (decryptedPassword != model.Password)
                                //if (decryptedPassword != model.Password && decryptedOldPassword != model.Password)
                                {
                                    TempData["ErrorMessage"] = "Incorrect password.";
                                    return RedirectToAction(nameof(Index));
                                }
                                string dbIp = reader["Lip"]?.ToString();
                                string dbPcName = reader["PC_NAME"]?.ToString();
                                string dbPcName2 = reader["PC_NAME2"]?.ToString();
                                string dbPcName3 = reader["PC_NAME3"]?.ToString();

                                // Check if IP matches
                                //if (dbIp != localIp)
                                //{
                                //    TempData["ErrorMessage"] = $"Access denied. IP mismatch: {localIp}";
                                //    return RedirectToAction(nameof(Index));
                                //}
                                // Check if PC Name matches

                                //if (dbPcName != pcName && dbPcName2 != pcName && dbPcName3 != pcName)
                                //{
                                //    TempData["ErrorMessage"] = $"Access denied. PC Name mismatch: {pcName}";
                                //    return RedirectToAction(nameof(Index));
                                //}
                                int isLoggedIn = reader["IsLoggedIn"] != DBNull.Value ? Convert.ToInt32(reader["IsLoggedIn"]) : 0;
                                string code = reader["CODE"].ToString();
                                string userName = reader["USER_NAME"].ToString();
                                string USER_LEVEL = reader["USER_LEVEL"].ToString();
                                string CompCode = model.CompanyCode;
                                DateTime loginDate = model.LoginDate;
                                reader.Close(); 

                                // Step 3: Check if the user has the necessary menu access

                                HttpContext.Session.SetString("COMP_CODE", CompCode);
                                using (SqlConnection con2 = _dbConnection.GetErpConnection())
                                {
                                    if (con2.State != ConnectionState.Open)
                                    {
                                        con2.Open();
                                    }
                                    string menuQuery = "SELECT 1 FROM user_menu WHERE USER_CODE = @UserMasterCode AND COMP_CODE = @CompanyCode AND YEAR_CODE = @YearCode";
                                    using (SqlCommand cmd2 = new SqlCommand(menuQuery, con2))
                                    {
                                        cmd2.Parameters.AddWithValue("@UserMasterCode", model.UserMasterCode);
                                        cmd2.Parameters.AddWithValue("@CompanyCode", model.CompanyCode);
                                        cmd2.Parameters.AddWithValue("@YearCode", model.FinancialYear);
                                        var menuResult = cmd2.ExecuteScalar();

                                        //if (menuResult == null)
                                        //{
                                        //    TempData["ErrorMessage"] = $"Year code is not Found";
                                        //    return RedirectToAction(nameof(Index)); 
                                        //}
                                    }
                                }
                                // Step 4: Handle login status
                                using (SqlConnection con3 = _dbConnection.GetConDbConnection())
                                {
                                    if (con3.State != ConnectionState.Open)
                                    {
                                        con3.Open();
                                    }
                                    if (isLoggedIn == 1)
                                    {
                                        string resetLoginStatusQuery = "UPDATE USER_MAST SET IsLoggedIn = 0 WHERE CODE = @Code AND COMP_CODE = @CompCode";
                                        using (SqlCommand resetCmd = new SqlCommand(resetLoginStatusQuery, con3))
                                        {
                                            resetCmd.Parameters.AddWithValue("@Code", code);
                                            resetCmd.Parameters.AddWithValue("@CompCode", CompCode);
                                            resetCmd.ExecuteNonQuery();
                                        }
                                    }
                                    // Proceed to login: set IsLoggedIn = 1
                                    string updateLoginStatusQuery = "UPDATE USER_MAST SET IsLoggedIn = 1 WHERE CODE = @Code AND COMP_CODE = @CompCode";
                                    using (SqlCommand updateCmd = new SqlCommand(updateLoginStatusQuery, con3))
                                    {
                                        updateCmd.Parameters.AddWithValue("@Code", code);
                                        updateCmd.Parameters.AddWithValue("@CompCode", CompCode);
                                        updateCmd.ExecuteNonQuery();
                                    }
                                }
                                // Step 5: Store in session
                                HttpContext.Session.SetString("USER_NAME", userName);
                                HttpContext.Session.SetString("CODE", code);
                                HttpContext.Session.SetString("YEAR_CODE", model.FinancialYear);
                                HttpContext.Session.SetString("USER_LEVEL", USER_LEVEL);
                                HttpContext.Session.SetString("SessionYearCode", model.FinancialYear.ToString());
                                HttpContext.Session.SetString("SessionLogindate", loginDate.ToString("o"));
                                return RedirectToAction("Index", "Dashboard");
                            }
                            else
                            {
                                HttpContext.Session.Clear(); 
                                TempData["ErrorMessage"] = "User not found.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Login error: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        public IActionResult Logout()
        {
            string code = HttpContext.Session.GetString("CODE");
            string compCode = HttpContext.Session.GetString("COMP_CODE");

            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(compCode))
            {
                using (SqlConnection con = _dbConnection.GetConDbConnection())
                {
                    string logoutQuery = "UPDATE USER_MAST SET IsLoggedIn = 0 WHERE CODE = @Code AND COMP_CODE = @CompCode";
                    using (SqlCommand cmd = new SqlCommand(logoutQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Code", code);
                        cmd.Parameters.AddWithValue("@CompCode", compCode);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
        // Decrypt function
        //private string Decrypt(string encrypted)
        //{
        //    StringBuilder decrypted = new StringBuilder();
        //    foreach (char letter in encrypted)
        //    {
        //        decrypted.Append((char)(255 - (int)letter));
        //    }
        //    return decrypted.ToString();
        //}

        //private string Decrypt(string encrypted)
        //{
        //    if (string.IsNullOrEmpty(encrypted))
        //        return string.Empty;

        //    // Convert encrypted string to byte array (using your logic)
        //    byte[] decryptedBytes = new byte[encrypted.Length];

        //    for (int i = 0; i < encrypted.Length; i++)
        //    {
        //        decryptedBytes[i] = (byte)(255 - (byte)encrypted[i]);
        //    }

        //    // Decode bytes as UTF-8 string (or ASCII if you prefer)
        //    return Encoding.UTF8.GetString(decryptedBytes);
        //}

        private string Decrypt(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted))
                return string.Empty;

            byte[] decryptedBytes = new byte[encrypted.Length];

            for (int i = 0; i < encrypted.Length; i++)
            {
                byte originalByte = (byte)encrypted[i];
                byte decryptedByte = (byte)(255 - originalByte);

                Console.WriteLine($"Encrypted char: '{encrypted[i]}' -> ASCII: {originalByte} | Decrypted byte: {decryptedByte}");

                decryptedBytes[i] = decryptedByte;
            }

            string result = Encoding.UTF8.GetString(decryptedBytes);
            Console.WriteLine("Final Decrypted String: " + result);

            return result;
        }
        public JsonResult GeUserNameddl()
        {
            List<object> username = new List<object>();
            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                string query = "Select code, USER_NAME From USER_MAST where ACTIVE=1 ORDER BY USER_NAME";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open(); 
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    username.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["USER_NAME"].ToString()
                    });
                }
            }
            return Json(username);
        }
        [HttpPost]
        public JsonResult GetCompanyIDByUser(string userId)
        {
            List<object> company = new List<object>();
            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                string query = "SELECT sub.COMP_CODE as code, comp.NAME as name FROM SUBUSER_MAST sub INNER JOIN COMP_MAST comp ON sub.COMP_CODE = comp.CODE WHERE sub.USER_CODE = @UserId and comp.active=1";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId); // Match this with the query
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    company.Add(new
                    {
                        Value = reader["code"].ToString(),
                        Text = reader["name"].ToString()
                    });
                }
            }
            return Json(company);
        }
        //ddlFinancialYear start block
        public JsonResult GeddlFinancialYear()
        {
            List<object> financialYearList = new List<object>();
            string currentFinancialYearText = "";

            // Calculate current financial year text based on DateTime.Now
            var now = DateTime.Now;
            if (now.Month >= 4)
                currentFinancialYearText = $"{now.Year}-{now.Year + 1}";
            else
                currentFinancialYearText = $"{now.Year - 1}-{now.Year}";

            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                string query = "SELECT code, CURR_YEAR FROM YEAR_MAST ORDER BY code DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    financialYearList.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["CURR_YEAR"].ToString()
                    });
                }
            }
            // Return both the list and the current year string
            return Json(new
            {
                financialYears = financialYearList,
                currentYearText = currentFinancialYearText
            });
        }

        [HttpPost]
        public IActionResult GetYearID(string financialYear)
        {
            List<object> result = new List<object>();

            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                string query = "SELECT code, CONVERT(varchar, END_DATE, 103) AS END_DATE  FROM YEAR_MAST WHERE code = @code";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@code", financialYear);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new
                    {
                        Value = reader["code"].ToString(),
                        Text = reader["END_DATE"].ToString()
                    });
                }
            }
            return Json(result);
        }


    }
}
