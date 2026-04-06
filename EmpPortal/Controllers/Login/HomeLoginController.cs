using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;

namespace EmpPortal.Controllers.Login
{
    public class HomeLoginController : Controller
    {

        private readonly IWebHostEnvironment _env;
        private readonly DataBaseConnection _dbConnection;
        private readonly string _connectionString = "Data Source=118.139.164.161;Initial Catalog=Hrms_db;Persist Security Info=True;User ID=noida;Password=Kwalityy@214#;Trust Server Certificate=True";
        //private readonly string _connectionString = "Data Source=192.168.20.51;Initial Catalog=ERPDB;Persist Security Info=True;User ID=sa;Password=Pass@123;Trust Server Certificate=True";

        public HomeLoginController(DataBaseConnection dbConnection)
        {
            //_dbConnection = dbConnection;
        }

        public IActionResult Index()
        {
            return View("~/Views/HomeLogin/Index.cshtml");
        }

        [HttpPost]
        public IActionResult RegisterVisitor(VisitorLoginModel model)
        {
            if (string.IsNullOrEmpty(model.Name) ||
                string.IsNullOrEmpty(model.VisitorMobile) ||
                string.IsNullOrEmpty(model.Password))
            {
                return Json(new { success = false, message = "All fields are required" });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    // Check if name or mobile already exists
                    string checkQuery = @"SELECT COUNT(*) FROM VisitorLogin
                                  WHERE Name = @Name OR VisitorMobile = @VisitorMobile";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", model.Name);
                        checkCmd.Parameters.AddWithValue("@VisitorMobile", model.VisitorMobile);

                        con.Open();
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            return Json(new { success = false, message = "Name or Mobile already exists" });
                        }
                    }

                    // Insert if not exists
                    string insertQuery = @"INSERT INTO VisitorLogin (Name, VisitorMobile, [Password])
                                   VALUES (@Name, @VisitorMobile, @Password)";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Name", model.Name);
                        cmd.Parameters.AddWithValue("@VisitorMobile", model.VisitorMobile);
                        cmd.Parameters.AddWithValue("@Password", model.Password);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "Database error" });
            }
        }

        //[HttpPost]
        //public IActionResult RegisterVisitor(VisitorLoginModel model)
        //{
        //    if (string.IsNullOrEmpty(model.Name) ||
        //        string.IsNullOrEmpty(model.VisitorMobile) ||
        //        string.IsNullOrEmpty(model.Password))
        //    {
        //        return Json(new { success = false, message = "All fields are required" });
        //    }

        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(_connectionString))
        //        {
        //            string query = @"INSERT INTO VisitorLogin
        //                     (Name, VisitorMobile, [Password])
        //                     VALUES
        //                     (@Name, @VisitorMobile, @Password)";

        //            using (SqlCommand cmd = new SqlCommand(query, con))
        //            {
        //                cmd.Parameters.AddWithValue("@Name", model.Name);
        //                cmd.Parameters.AddWithValue("@VisitorMobile", model.VisitorMobile);
        //                cmd.Parameters.AddWithValue("@Password", model.Password); 

        //                con.Open();
        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        return Json(new { success = true });
        //    }
        //    catch (SqlException ex)
        //    {
        //        if (ex.Number == 2627) // UNIQUE constraint (mobile already exists)
        //        {
        //            return Json(new { success = false, message = "Mobile number already registered" });
        //        }

        //        return Json(new { success = false, message = "Database error" });
        //    }
        //}

        [HttpPost]
        public IActionResult VerifyVisitorLogin(string username, string password)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    string query = @"SELECT VisitorID 
                             FROM VisitorLogin
                             WHERE (Name = @Username)
                               AND [Password] = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);

                        con.Open();
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            HttpContext.Session.SetString("VISITOR_NAME", username);
                            return Json(new { success = true });
                        }
                    }
                }

                return Json(new { success = false, message = "Invalid username or password" });
            }
            catch
            {
                return Json(new { success = false, message = "Database error" });
            }
        }



        public class VisitorLoginModel
        {
            public string Name { get; set; }
            public string VisitorMobile { get; set; }
            public string Password { get; set; }
        }
    }
}
