using Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class EmployeeDashboardController : Controller
    {
        private readonly DataBaseConnection _dbConnection;

        public EmployeeDashboardController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {       
            return View("~/Views/EmployeePortal/EmployeeDashboard/Index.cshtml");
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]   //New code
        public IActionResult EmployeeDetails()
        {
            string mobile = HttpContext.Session.GetString("MOBILE");
            string compCode = HttpContext.Session.GetString("COMP_CODE");

            if (string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(compCode))  //New code
            {
                return Json(new { success = false, message = "Session expired" });
            }

            EmployeeDetailsDto emp = null;
            List<HolidayDto> holidays = new List<HolidayDto>();

            string empQuery = @"
                SELECT  emp.EMP_ID,emp.Code,emp.NAME AS FullName,des.NAME AS DesignationName,dep.NAME AS DepartmentName,emp.DOB as DOB,emp.JOIN_DATE,emp.DOM as DOM, Emp.EMAIL , emp.imagebyte,emp.active
                FROM EMP_MAST emp  LEFT JOIN DESG_MAST des   ON emp.DESG_CODE = des.CODE  AND emp.COMP_CODE = des.COMP_CODE
                LEFT JOIN DEPT_MAST dep  ON emp.DEPT_CODE = dep.CODE  AND emp.COMP_CODE = dep.COMP_CODE    WHERE emp.MOBILE = @Mobile  AND emp.COMP_CODE = @compCode";


            string holidayQuery = @" SELECT Name, HOLIDAY_DATE FROM HOLIDAY_MAST WHERE HOLIDAY_DATE >= DATEFROMPARTS(YEAR(GETDATE()), 1, 1) ORDER BY HOLIDAY_DATE DESC;";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // 🔹 Employee Details
                using (SqlCommand cmd = new SqlCommand(empQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@compCode", compCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            emp = new EmployeeDetailsDto
                            {
                                EMP_ID = Convert.ToInt32(dr["EMP_ID"]),
                                active = Convert.ToInt32(dr["active"]),
                                Code = dr["Code"].ToString(),
                                FullName = dr["FullName"].ToString(),
                                DesignationName = dr["DesignationName"].ToString(),
                                DepartmentName = dr["DepartmentName"].ToString(),
                                DOB = dr["DOB"] == DBNull.Value ? null : Convert.ToDateTime(dr["DOB"]),
                                JOIN_DATE = dr["JOIN_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["JOIN_DATE"]),
                                DOM = dr["DOM"] == DBNull.Value ? null : Convert.ToDateTime(dr["DOM"]),
                                Email = dr["EMAIL"].ToString(),
                                imagebyte = dr["imagebyte"] == DBNull.Value ? null : Convert.ToBase64String((byte[])dr["imagebyte"])
                            };
                            HttpContext.Session.SetInt32("EMP_ID", emp.EMP_ID);
                        }
                    }
                }
                // 🔹 Holiday List
                using (SqlCommand cmd = new SqlCommand(holidayQuery, con))
                {
                    cmd.Parameters.AddWithValue("@compCode", compCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            holidays.Add(new HolidayDto
                            {
                                Name = dr["Name"].ToString(),
                                HolidayDate = Convert.ToDateTime(dr["HOLIDAY_DATE"])
                            });
                        }
                    }
                }
            }
            var response = new EmployeeWithHolidayResponse
            {
                Employee = emp,
                Holidays = holidays
            };

            return Json(response);
        }

        [HttpGet]
        public IActionResult GetLeaveBalances()
        {
            int? empId = HttpContext.Session.GetInt32("EMP_ID");
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");

            if (empId == null || string.IsNullOrEmpty(compCodeStr))
                return Json(new { success = false });

            int compCode = Convert.ToInt32(compCodeStr);

            DateTime payStartDate = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime payEndDate = new DateTime(DateTime.Now.Year, 12, 31);

            LeaveBalanceDto leave = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("usp_GetEmpLeaveBalances", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompCode", compCode);
                cmd.Parameters.AddWithValue("@EmpCode", empId);
                cmd.Parameters.AddWithValue("@PaySDate", payStartDate);
                cmd.Parameters.AddWithValue("@PayEDate", payEndDate);
                cmd.Parameters.AddWithValue("@VNo", DBNull.Value);

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        leave = new LeaveBalanceDto
                        {
                            ELBal = Convert.ToDecimal(dr["ELBal"]),
                            CLBal = Convert.ToDecimal(dr["CLBal"]),
                            SLBal = Convert.ToDecimal(dr["SLBal"]),
                            ELApplied = Convert.ToDecimal(dr["ELApplied"]),
                            CLApplied = Convert.ToDecimal(dr["CLApplied"]),
                            SLApplied = Convert.ToDecimal(dr["SLApplied"])
                        };
                    }
                }
            }

            return Json(new { success = true, data = leave });
        }

        [HttpGet]
        public IActionResult GetLeaveBalance(int leaveCode)
        {
            int? empId = HttpContext.Session.GetInt32("EMP_ID");
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");
            //int? empId = 10616;


            if (!empId.HasValue || string.IsNullOrEmpty(compCodeStr))
                return Json(new { success = false, message = "Session expired" });

            int compCode = Convert.ToInt32(compCodeStr);

            int balCL = 0, balEL = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@" SELECT ISNULL(BAL_CL,0) AS BAL_CL,
                ISNULL(BAL_EL,0) AS BAL_EL FROM PAY_LEAVE WHERE EMP_CODE = @EMP_CODE AND COMP_CODE = @COMP_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@EMP_CODE", empId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            balCL = Convert.ToInt32(dr["BAL_CL"]);
                            balEL = Convert.ToInt32(dr["BAL_EL"]);
                        }
                    }
                }
            }
            return Json(new
            {
                success = true,
                balCL,
                balEL
            });
        }
        [HttpPost]
        public IActionResult SubmitLeaveRequest([FromBody] LeaveRequestModel model)
        {
            int? empId = HttpContext.Session.GetInt32("EMP_ID");
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");

            if (!empId.HasValue || string.IsNullOrEmpty(compCodeStr))
                return Json(new { success = false, message = "Session expired" });

            int compCode = Convert.ToInt32(compCodeStr);

            int result = 0;
            string message = "";
            int vNo = 0;

            string getVnoQuery = @" SELECT ISNULL(MAX(V_NO), 0) + 1 FROM PAY_LEAVE WHERE V_TYPE = @vtype
            AND COMP_CODE = @comp AND BRANCH_CODE = @branch AND YEAR_CODE = @year";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(getVnoQuery, con))
            {
                cmd.Parameters.AddWithValue("@vtype", "LEAV");
                cmd.Parameters.AddWithValue("@comp", compCode);
                cmd.Parameters.AddWithValue("@branch", 1);
                cmd.Parameters.AddWithValue("@year", 8); 

                con.Open();
                vNo = Convert.ToInt32(cmd.ExecuteScalar());
            }
            string DOC_ID = "LEAV" + vNo;

            // 🔹 Save Leave
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("usp_SavePayLeaveEmpPortal", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Mode", "I");
                cmd.Parameters.AddWithValue("@YEAR_CODE", 8); 
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                cmd.Parameters.AddWithValue("@V_TYPE", "LEAV");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@V_DATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                cmd.Parameters.AddWithValue("@EMP_CODE", empId.Value);
                cmd.Parameters.AddWithValue("@FROM_DATE", model.FromDate);
                cmd.Parameters.AddWithValue("@TO_DATE", model.ToDate);
                cmd.Parameters.AddWithValue("@LEAVE_CODE", model.LeaveType);
                cmd.Parameters.AddWithValue("@LEAVE_TYPE", model.LeaveTypeName);
                cmd.Parameters.AddWithValue("@LEAVE_REASON", model.Remarks);
                cmd.Parameters.AddWithValue("@UUSER", empId.Value);

                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        result = Convert.ToInt32(dr["result"]);
                        message = dr["Message"].ToString();
                    }
                }
            }
            if (result <= 0) return Json(new { success = false, message });
            return Json(new
            {
                success = true,
                message,
                vNo
            });
        }
         
        [HttpPost]
        public IActionResult UpdateLeave(int index, string fromDate, string toDate, string leaveType, int days, string status, string DocID)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string sql = @"UPDATE PAY_LEAVE 
                    SET FROM_DATE = @fromDate,
                    TO_DATE = @toDate,
                    LEAVE_TYPE = @leaveType , FAPROV_STATUS = 'Pending' 
                    WHERE DOC_ID = @DocID";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@fromDate", fromDate);
                        cmd.Parameters.AddWithValue("@toDate", toDate);
                        cmd.Parameters.AddWithValue("@leaveType", leaveType);
                        cmd.Parameters.AddWithValue("@DocID", DocID);

                        con.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "No record updated" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateImage(IFormFile file)
        {
            try
            {
                int? empId = HttpContext.Session.GetInt32("EMP_ID");
 
                if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected" });

                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    imageBytes = ms.ToArray();
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string sql = "UPDATE EMP_MAST SET imagebyte = @image WHERE EMP_ID = @EMP_ID";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@image", imageBytes);
                        cmd.Parameters.AddWithValue("@EMP_ID", empId);

                        con.Open();
                        int rows = cmd.ExecuteNonQuery();

                        return Json(new { success = rows > 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult GetLeaveHistoryData(string selecteddate)
        {
            int? empId = HttpContext.Session.GetInt32("EMP_ID");
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");

            List<object> leaveList = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@" 
                    SELECT DOC_ID, FROM_DATE, TO_DATE, LEAVE_TYPE, FAPROV_REMARKS, FAPROV_STATUS 
                    FROM PAY_LEAVE 
                    WHERE EMP_CODE = @EMP_CODE AND COMP_CODE = @COMP_CODE AND FORMAT(FROM_DATE,'yyyy-MM') = @selecteddate", con))               
                
                {

                   if(selecteddate == null)
                    {       
                         selecteddate = (string.IsNullOrEmpty(selecteddate) ? DateTime.Now : DateTime.Parse(selecteddate)).AddMonths(-1).ToString("yyyy-MM");
                    }
                
                    cmd.Parameters.AddWithValue("@EMP_CODE", empId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCodeStr);
                    // If selecteddate is null, use DateTime.Now
                    cmd.Parameters.AddWithValue("@selecteddate", selecteddate);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())   
                        {
                            leaveList.Add(new
                            {
                                FromDate = Convert.ToDateTime(dr["FROM_DATE"]).ToString("yyyy-MM-dd"),
                                ToDate = Convert.ToDateTime(dr["TO_DATE"]).ToString("yyyy-MM-dd"),
                                LeaveType = dr["LEAVE_TYPE"].ToString(),
                                DOC_ID = dr["DOC_ID"].ToString(),
                                Remarks = dr["FAPROV_REMARKS"].ToString(),
                                Status = dr["FAPROV_STATUS"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                success = true,
                data = leaveList
            });
        }
        public class EmployeeDetailsDto
        {
            public int EMP_ID { get; set; }
            public int active { get; set; }
            public string Code { get; set; }
            public string FullName { get; set; }
            public string DesignationName { get; set; }
            public string DepartmentName { get; set; }
            public DateTime? DOB { get; set; }
            public DateTime? JOIN_DATE { get; set; }
            public DateTime? DOM { get; set; }
            public string? Email { get; set; }
            public string? imagebyte { get; set; }
        }
        public class HolidayDto
        {
            public string Name { get; set; }
            public DateTime HolidayDate { get; set; }
        }
        public class EmployeeWithHolidayResponse
        {
            public EmployeeDetailsDto Employee { get; set; }
            public List<HolidayDto> Holidays { get; set; }
        }
        public class LeaveBalanceDto
        {
            public decimal ELBal { get; set; }
            public decimal CLBal { get; set; }
            public decimal SLBal { get; set; }
            public decimal ELApplied { get; set; }
            public decimal CLApplied { get; set; }
            public decimal SLApplied { get; set; }
        }
        public class LeaveRequestModel
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public int LeaveType { get; set; }
            public string LeaveTypeName { get; set; }
            public string Remarks { get; set; }
        }

    }
}
