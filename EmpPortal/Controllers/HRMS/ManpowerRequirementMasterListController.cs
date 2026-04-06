using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class ManpowerRequirementMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ManpowerRequirementMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/HRMS/ManpowerRequirementMasterList/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetManpowerRequirementMasterList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("usp_ManpowerRequirementMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@SearchTerm",
                        string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                code = reader["CODE"]?.ToString() ?? "",
                                deptName = reader["DEPT_NAME"]?.ToString() ?? "",
                                desgName = reader["DESG_NAME"]?.ToString() ?? "",
                                placeCode = reader["PLACE_CODE"]?.ToString() ?? "",
                                nos = reader["NOS"]?.ToString() ?? "",
                                remarks = reader["REMARKS"]?.ToString() ?? "",
                                active = reader["ACTIVE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACTIVE"]),
                                faprovStatus = reader["FAPROV_STATUS"]?.ToString() ?? "",
                                faprovRemarks = reader["FAPROV_REMARKS"]?.ToString() ?? "",
                                uuser = reader["UUSER"]?.ToString() ?? "",
                                udate = reader["UDATE"] == DBNull.Value ? "" :
                                        Convert.ToDateTime(reader["UDATE"]).ToString("dd-MM-yyyy")
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader.GetInt32(0);
                        }
                    }
                }
            }
            return Json(new
            {
                groups = results,
                totalCount = totalCount
            });
        }

        [HttpPost]
        public JsonResult DeleteID(int ID)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM PAY_NEWEMPREQ WHERE CODE = @CODE AND COMP_CODE = @COMP_CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@CODE", ID);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = "Record deleted successfully." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Record not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
