using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class ManpowerRequirementMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly Controllers.DropdownService.DropdownService _dropdownService;

        public ManpowerRequirementMasterController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            Controllers.DropdownService.DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
        }
        public IActionResult Index()
        {
            return View("~/Views/HRMS/ManpowerRequirementMaster/Index.cshtml");
        }
        public JsonResult GetddlDepartment()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = @"SELECT Code, Name FROM DEPT_MAST WHERE COMP_CODE=" + gv.PubCompCode + " ORDER BY Name ASC";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        public JsonResult GetddlDesignation()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = @"SELECT Code, Name FROM DESG_MAST WHERE COMP_CODE=" + gv.PubCompCode + " ORDER BY Name ASC";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        // ================= SAVE (INSERT / UPDATE MULTIPLE ROWS) =================
        [HttpPost]
        public IActionResult SaveManpowerRequirement([FromBody] List<ManpowerRequirementModel> model)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();
                try
                {
                    foreach (var item in model)
                    {
                        if (item.Code == 0) // INSERT
                        {
                            SqlCommand maxCmd = new SqlCommand(
                                "SELECT ISNULL(MAX(CODE),0)+1 FROM PAY_NEWEMPREQ WHERE COMP_CODE=@COMP",
                                con, transaction);

                            maxCmd.Parameters.AddWithValue("@COMP", gv.PubCompCode);
                            int newCode = Convert.ToInt32(maxCmd.ExecuteScalar());

                            SqlCommand insertCmd = new SqlCommand(@"
                                INSERT INTO PAY_NEWEMPREQ
                                (COMP_CODE, M_TYPE, CODE, DEPT_CODE, DESG_CODE, PLACE_CODE, NOS,
                                 SalaryRange, Qualification, Experience,
                                 Reason, ACTIVE, REMARKS)
                                VALUES
                                (@COMP, @M_TYPE, @CODE, @DEPT, @DESG, @PLACE, @NOS,
                                 @SALARY, @QUAL, @EXP,
                                 @REASON, @ACTIVE, @REMARKS)", con, transaction);

                            insertCmd.Parameters.AddWithValue("@COMP", gv.PubCompCode);
                            insertCmd.Parameters.AddWithValue("@M_TYPE", "NREQ");
                            insertCmd.Parameters.AddWithValue("@CODE", newCode);
                            insertCmd.Parameters.AddWithValue("@DEPT", item.DepartmentCode);
                            insertCmd.Parameters.AddWithValue("@DESG", item.DesignationCode);
                            insertCmd.Parameters.AddWithValue("@PLACE", item.WorkPlace);
                            insertCmd.Parameters.AddWithValue("@NOS", item.NoPosition);
                            insertCmd.Parameters.AddWithValue("@SALARY", item.SalaryRange ?? "");
                            insertCmd.Parameters.AddWithValue("@QUAL", item.Qualification ?? "");
                            insertCmd.Parameters.AddWithValue("@EXP", item.Experience ?? "");
                            insertCmd.Parameters.AddWithValue("@REASON", item.Reason ?? "");
                            insertCmd.Parameters.AddWithValue("@ACTIVE", item.ActiveStatus);
                            insertCmd.Parameters.AddWithValue("@REMARKS", item.Remarks ?? "");

                            insertCmd.ExecuteNonQuery();
                        }
                        else // UPDATE
                        {
                            SqlCommand updateCmd = new SqlCommand(@"
                                UPDATE PAY_NEWEMPREQ SET
                                    DEPT_CODE = @DEPT,
                                    DESG_CODE = @DESG,
                                    PLACE_CODE = @PLACE,
                                    NOS = @NOS,
                                    SalaryRange = @SALARY,
                                    Qualification = @QUAL,
                                    Experience = @EXP,
                                    Reason = @REASON,
                                    ACTIVE = @ACTIVE,
                                    REMARKS = @REMARKS
                                WHERE COMP_CODE = @COMP AND CODE = @CODE", con, transaction);

                            updateCmd.Parameters.AddWithValue("@COMP", gv.PubCompCode);
                            updateCmd.Parameters.AddWithValue("@CODE", item.Code);
                            updateCmd.Parameters.AddWithValue("@DEPT", item.DepartmentCode);
                            updateCmd.Parameters.AddWithValue("@DESG", item.DesignationCode);
                            updateCmd.Parameters.AddWithValue("@PLACE", item.WorkPlace);
                            updateCmd.Parameters.AddWithValue("@NOS", item.NoPosition);
                            updateCmd.Parameters.AddWithValue("@SALARY", item.SalaryRange ?? "");
                            updateCmd.Parameters.AddWithValue("@QUAL", item.Qualification ?? "");
                            updateCmd.Parameters.AddWithValue("@EXP", item.Experience ?? "");
                            updateCmd.Parameters.AddWithValue("@REASON", item.Reason ?? "");
                            updateCmd.Parameters.AddWithValue("@ACTIVE", item.ActiveStatus);
                            updateCmd.Parameters.AddWithValue("@REMARKS", item.Remarks ?? "");

                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }
        [HttpGet]
        public IActionResult GetManpowerRequirementMasterCode(int code)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            List<ManpowerRequirementModel> list = new List<ManpowerRequirementModel>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT CODE, DEPT_CODE, DESG_CODE, PLACE_CODE, NOS,
                                 SalaryRange, Qualification, Experience,
                                 Reason, ACTIVE, REMARKS
                                 FROM PAY_NEWEMPREQ
                                 WHERE COMP_CODE=@COMP AND CODE=@CODE";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@COMP", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@CODE", code);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new ManpowerRequirementModel
                    {
                        Code = Convert.ToInt32(dr["CODE"]),
                        DepartmentCode = Convert.ToInt32(dr["DEPT_CODE"]),
                        DesignationCode = Convert.ToInt32(dr["DESG_CODE"]),
                        WorkPlace = Convert.ToInt32(dr["PLACE_CODE"]),
                        NoPosition = Convert.ToInt32(dr["NOS"]),
                        SalaryRange = dr["SalaryRange"]?.ToString(),
                        Qualification = dr["Qualification"]?.ToString(),
                        Experience = dr["Experience"]?.ToString(),
                        Reason = dr["Reason"]?.ToString(),
                        ActiveStatus = Convert.ToInt32(dr["ACTIVE"]),
                        Remarks = dr["REMARKS"]?.ToString()
                    });
                }
            }

            return Json(list);
        }
    }
    public class ManpowerRequirementModel
    {
        public int Code { get; set; }
        public int DepartmentCode { get; set; }
        public int DesignationCode { get; set; }
        public int WorkPlace { get; set; }
        public int NoPosition { get; set; }
        public int ActiveStatus { get; set; }
        public string SalaryRange { get; set; }
        public string Qualification { get; set; }
        public string Experience { get; set; }
        public string Reason { get; set; }
        public string Remarks { get; set; }
    }
}