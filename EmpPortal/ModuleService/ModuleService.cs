using System.Reflection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.ModuleService
{
    public class ModuleService
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public ModuleService(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public List<Module> GetAllModules()
        {
            var modules = new List<Module>();
            using var conn = _dbConnection.GetErpConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT Code, DISPLAY_NAME FROM MODULE_MAST", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                modules.Add(new Module
                {
                    Code = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : 0,
                    DisplayName = reader["DISPLAY_NAME"]?.ToString() ?? string.Empty
                });
            }
            return modules;
        }
        public List<MenuModule> GetMenuMaster()
        {
            var menumodules = new List<MenuModule>();
            var globals = _globalVariableService.GetGlobalVariables();
            int userLevel = GetUserLevel() ?? 0;

            string menuQuery = userLevel == 1
                ? "SELECT CODE, MODULE_CODE, DISPLAY_NAME, WebFORM_NAME FROM MENU_MAST ORDER BY DISPLAY_NAME ASC"
                : "SELECT CODE, MODULE_CODE, DISPLAY_NAME, WebFORM_NAME FROM MENU_MAST WHERE DISPLAY_NAME NOT IN ('User Authorizations') ORDER BY DISPLAY_NAME ASC";
            using var conn = _dbConnection.GetErpConnection();
            conn.Open();
            using var cmd = new SqlCommand(menuQuery, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                menumodules.Add(new MenuModule
                {
                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                    MODULE_CODE = reader["MODULE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MODULE_CODE"]) : 0,
                    DISPLAY_NAME = reader["DISPLAY_NAME"]?.ToString() ?? string.Empty,
                    WebFORM_NAME = reader["WebFORM_NAME"]?.ToString() ?? string.Empty
                });
            }
            return menumodules;
        }
        public List<UserMenuPermission> GetUserMenuPermissions()    
        {
            var userPermissions = new List<UserMenuPermission>();
            var globals = _globalVariableService.GetGlobalVariables();
            int userLevel = GetUserLevel() ?? 0;
            using var conn = _dbConnection.GetErpConnection();
            conn.Open();
            string query = userLevel == 1
            //? @"SELECT MENU_CODE, a.MODULE_CODE, USER_CODE, _ACCESS, _ADD, _EDIT, _DELETE, _PRINT, _EXPORT, _MAIL, _APPROVAL, _DOCDETAIL, MENU_NAME,MENU_TYPE,MENU_OPTION,b.DISPLAY_NAME 
            //FROM USER_MENU a inner join MENU_MAST b on a.MENU_CODE= b.CODE
            //WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND _ACCESS = 1"
            ? @"SELECT MENU_CODE, a.MODULE_CODE, USER_CODE, _ACCESS, _ADD, _EDIT, _DELETE, _PRINT, _EXPORT, _MAIL, _APPROVAL, _DOCDETAIL, MENU_NAME,MENU_TYPE,MENU_OPTION,b.DISPLAY_NAME 
            FROM USER_MENU a inner join MENU_MAST b on a.MENU_CODE= b.CODE WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND _ACCESS = 1 
            AND b.MAINMENU_CODE > 0 ORDER BY b.MODULE_CODE, b.MAINMENU_CODE, b.MENU_OPTION;"
            : @"SELECT MENU_CODE, a.MODULE_CODE, USER_CODE, _ACCESS, _ADD, _EDIT, _DELETE, _PRINT, _EXPORT, _MAIL, _APPROVAL, _DOCDETAIL, MENU_NAME,MENU_TYPE,MENU_OPTION,b.DISPLAY_NAME 
            FROM USER_MENU a inner join MENU_MAST b on a.MENU_CODE= b.CODE
            WHERE USER_CODE = @UserCode AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND _ACCESS = 1 AND B.WebFORM_NAME<>'' order by module_code,MENU_TYPE,MENU_OPTION";

            using var cmd = new SqlCommand(query, conn);
            if (userLevel != 1)
                cmd.Parameters.AddWithValue("@UserCode", globals.PubUserId);

            cmd.Parameters.AddWithValue("@COMP_CODE", globals.PubCompCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globals.PubFYearCode);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                userPermissions.Add(new UserMenuPermission
                {
                    MENU_CODE = reader["MENU_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MENU_CODE"]) : 0,
                    MODULE_CODE = reader["MODULE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MODULE_CODE"]) : 0,
                    USER_CODE = reader["USER_CODE"] != DBNull.Value ? Convert.ToInt32(reader["USER_CODE"]) : 0,
                    _ACCESS = reader["_ACCESS"] != DBNull.Value ? Convert.ToInt32(reader["_ACCESS"]) : 0,
                    _ADD = reader["_ADD"] != DBNull.Value ? Convert.ToInt32(reader["_ADD"]) : 0,
                    _EDIT = reader["_EDIT"] != DBNull.Value ? Convert.ToInt32(reader["_EDIT"]) : 0,
                    _DELETE = reader["_DELETE"] != DBNull.Value ? Convert.ToInt32(reader["_DELETE"]) : 0,
                    _PRINT = reader["_PRINT"] != DBNull.Value ? Convert.ToInt32(reader["_PRINT"]) : 0,
                    _EXPORT = reader["_EXPORT"] != DBNull.Value ? Convert.ToInt32(reader["_EXPORT"]) : 0,
                    _MAIL = reader["_MAIL"] != DBNull.Value ? Convert.ToInt32(reader["_MAIL"]) : 0,
                    _APPROVAL = reader["_APPROVAL"] != DBNull.Value ? Convert.ToInt32(reader["_APPROVAL"]) : 0,
                    _DOCDETAIL = reader["_DOCDETAIL"] != DBNull.Value ? Convert.ToInt32(reader["_DOCDETAIL"]) : 0,
                    MENU_NAME = reader["MENU_NAME"]?.ToString() ?? string.Empty,
                    DISPLAY_NAME = reader["DISPLAY_NAME"]?.ToString() ?? string.Empty
                });
            }
            return userPermissions;
        }
        public int? GetUserLevel()
        {
            var globals = _globalVariableService.GetGlobalVariables();
            if (globals == null || globals.PubUserId == null)
                return null;
            using var conn = _dbConnection.GetConDbConnection();
            conn.Open();
            const string query = "SELECT USER_LEVEL FROM USER_MAST WHERE CODE = @UserCode";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserCode", globals.PubUserId ?? (object)DBNull.Value);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : (int?)null;
        }
        public string? GetCompanyName()
        {
            var globals = _globalVariableService.GetGlobalVariables();
            using var conn = _dbConnection.GetConDbConnection();
            conn.Open();
            const string query = "SELECT Name FROM COMP_MAST WHERE CODE = @UserCode";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserCode", globals.PubCompCode ?? (object)DBNull.Value);
            var companyName = cmd.ExecuteScalar();
            return companyName != null ? companyName.ToString() : null;
        }
        // Models
        public class Module
        {
            public int Code { get; set; }
            public string DisplayName { get; set; }
        }
        public class MenuModule
        {
            public int MODULE_CODE { get; set; }
            public string DISPLAY_NAME { get; set; }
            //public string FORM_NAME { get; set; }
            public string WebFORM_NAME { get; set; }
            public int CODE { get; set; }
        }
        public class UserMenuPermission
        {
            public int MODULE_CODE { get; set; }
            public int USER_CODE { get; set; }
            public int _ACCESS { get; set; }
            public int _ADD { get; set; }
            public int _EDIT { get; set; }
            public int _DELETE { get; set; }
            public int _PRINT { get; set; }
            public int _EXPORT { get; set; }
            public int _MAIL { get; set; }
            public int _APPROVAL { get; set; }
            public int _DOCDETAIL { get; set; }
            public string MENU_NAME { get; set; }
            public int MENU_CODE { get; set; }
            public string DISPLAY_NAME { get; set; }
        }
    }
}
