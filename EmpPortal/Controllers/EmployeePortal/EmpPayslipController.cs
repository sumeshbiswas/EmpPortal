using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class EmpPayslipController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        public EmpPayslipController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/EmpPayslip/Index.cshtml");
        }

        [HttpGet]
        public IActionResult PayslipDetails(DateTime vdate)
        {
            string Comcode = HttpContext.Session.GetString("COMP_CODE");
            int? EmpCode = HttpContext.Session.GetInt32("EMP_ID");

            DataTable dt = new DataTable();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("usp_paysalarydata", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@compcode", Comcode);
                //cmd.Parameters.AddWithValue("@empcode", EmpCode);
                cmd.Parameters.Add("@empcode", SqlDbType.Int).Value = EmpCode;
                cmd.Parameters.AddWithValue("@vdate", vdate);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
                return Json(new { success = false, message = "No payslip found for selected month" });

            var r = dt.Rows[0];

            var payslip = new
            {
                SDate = r["SDATE"],
                Name = r["NAME"],
                FatherName = r["FATHER_NAME"],
                CardNo = r["CARD_NO"],
                JoinDate = r["JOIN_DATE"],
                Department = r["DEPT_NAME"],
                Designation = r["DESG_NAME"],
                PFNo = r["PF_NO"],
                ESINo = r["ESI_NO"],
                Company = r["COMPANY_NAME"],
                WorkDay = r["WORKDAY"],
                LeaveDay = r["LEAVE_DAY"],
                MBasic = r["M_BASIC"],
                MHRA = r["M_HRA"],
                MConveyance = r["M_CONVEYANCE"],
                MOtherAllowance = r["M_OTHER_ALLOWANCE"],
                Basic = r["BASIC"],
                HRA = r["HRA"],
                FESTIVAL = r["FESTIVAL"],
                Conv = r["CONV"],
                PROF_TAX = r["PROF_TAX"],
                ADVSAL = r["ADVSAL"],
                OtherAllowance = r["OTHER_ALLOWANCE"],
                PF = r["PF"],
                ESI = r["ESI"],
                IncomeTax = r["INCOM_TAX"],
                Address = r["Address"]
            };

            //return Json(new { success = true, payslip });
            return Json(new { success = true, empId = EmpCode, payslip });
        }

    }
}
