using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Services
{
    public class MasterDataService : IMasterDataService
    {

        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MasterDataService(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public async Task<ApiResponse<object>> GetPlaceListAsync()
        {
            try
            {
                var placeList = await _dbHelper.GetJsonDataAsync($@"
                SELECT CODE, NAME 
                FROM PLACE_MAST 
                WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
                ORDER BY NAME");

                return new ApiResponse<object>
                {
                    status = true,
                    data = placeList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "Failed to load place list."
                };
            }
        }

        public async Task<ApiResponse<object>> GetItemListAsync()
        {
            try
            {
                var itemList = await _dbHelper.GetJsonDataAsync($@"
                SELECT CODE, NAME 
                FROM ITEM_MAST 
                WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}  
                ORDER BY NAME");

                return new ApiResponse<object>
                {
                    status = true,
                    data = itemList
                };
            }
            catch
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "Failed to load item list."
                };
            }
        }

        public async Task<ApiResponse<object>> GetRawItemListAsync()
        {
            try
            {
                var itemList = await _dbHelper.GetJsonDataAsync($@"
                SELECT ITEM_MAST.CODE, ITEM_MAST.NAME
                FROM ITEM_MAST left join ITEM_GROUP
                on ITEM_MAST.GROUP_CODE= ITEM_GROUP.CODE and ITEM_MAST.COMP_CODE= ITEM_GROUP.COMP_CODE
                WHERE ITEM_MAST.COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} and ITEM_GROUP.SALE_GROUP= 'Raw'
                order by ITEM_MAST.NAME ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = itemList
                };
            }
            catch
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "Failed to load item list."
                };
            }
        }

        public async Task<ApiResponse<object>> GetUserListAsync()
        {
            try
            {
                var userList = await _dbHelper.GetJsonDataAsync($@"
                SELECT CODE,NAME
                FROM EMP_MAST 
                WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = userList
                };
            }
            catch
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "Failed to load user list."
                };
            }
        }
        public async Task<ApiResponse<object>> GetStrengthListAsync()
        {
            try
            {

                var strqry = $@"select CODE, NAME from TENACITY_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}
                                order by NAME";
                var allList = await _dbHelper.GetJsonDataAsync(strqry);

                return new ApiResponse<object>
                {
                    status = true,
                    data = allList
                };

            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "Failed to load place list."
                };
            }

        }

        public async Task<ApiResponse<object>> GetStatusMastAsync()
        {
            try
            {
                var statuslist = await _dbHelper.GetJsonDataAsync($@"
                  Select CODE,NAME from STATUS_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME  ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = statuslist
                };

            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "Failed to load status list."
                };
            }

        }

        public async Task<ApiResponse<object>> GetItemDepartmentMastForProdAsync()
        {
            try
            {
                var departmentList = await _dbHelper.GetJsonDataAsync($@"
                select CODE, NAME from ITEMDEPT_MAST where TRAN_TYPE='Production' and COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME 
                ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = departmentList
                };

            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetMaxVNoAsync(string vType, string tableName)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";

                var yearParams = new Dictionary<string, object> { { "@YearCd", yearCode } };
                var vnoParams = new Dictionary<string, object>
            {
            { "@COMP_CODE", companyCode },
            { "@BRANCH_CODE", branchCode },
            { "@YEAR_CODE", yearCode },
            { "@V_TYPE", vType },
            { "@TableName", tableName }
            };

                string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>("sp_GetMaxVNo", vnoParams, isStoredProc: true);
                string year = await _dbHelper.GetExecuteScalarAsync<string>("SELECT dbo.fn_GetCurrentYear(@YearCd)", yearParams);
                var docId = (vType) + (year) + (nextVNo);
                var newVno = year + nextVNo;
                var docIdNoList = new { DocId = docId, VNo = newVno };

                return new ApiResponse<object>
                {
                    status = true,
                    data = docIdNoList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }

        }

        [HttpGet]
        public async Task<ApiResponse<object>> GetDocTypeAsync(string docType)
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync(@$"
                select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '') = '{docType}' ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = Doctype
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetEmployeeMastAsync()
        {
            try
            {
                var empList = await _dbHelper.GetJsonDataAsync($@"
            SELECT CODE, NAME ,DEPT_CODE
            FROM EMP_MAST 
            WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} 
              AND ACTIVE = 1  and RESIGN_DATE is null
            ORDER BY NAME
        ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = empList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetEmployeeDepartMastAsync()
        {
            try
            {
                var empDepartmastList = await _dbHelper.GetJsonDataAsync($@"
                select CODE, NAME from DEPT_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} order by NAME                 
                ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = empDepartmastList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetDesignationMastAsync()
        {
            try
            {
                var empDesignationList = await _dbHelper.GetJsonDataAsync($@"
                select CODE, NAME from DESG_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} order by NAME                 
                ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = empDesignationList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetShiftMastAsync()
        {
            try
            {
                var shiftList = await _dbHelper.GetJsonDataAsync($@"
            SELECT DISTINCT  SHIFT AS NAME 
            FROM SHIFT_MAST 
            WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} 
            ORDER BY NAME
        ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = shiftList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetDenierMastAsync()
        {
            try
            {
                var denierList = await _dbHelper.GetJsonDataAsync($@"
                select CODE, NAME from TAPE_NFABRIC_MAST 
                where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} 
                order by NAME 
                ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = denierList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetHodMastAsync()
        {
            try
            {
                var HODList = await _dbHelper.GetJsonDataAsync($@"
                select distinct EMP_CODE CODE, EMP_NAME NAME from PAYGATE_HOD                 
                where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} 
                order by EMP_NAME 
                ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = HODList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetBankMastAsync()
        {
            try
            {
                var BankList = await _dbHelper.GetJsonDataAsync(@$"
                select distinct CODE,NAME from BANK_MAST order by NAME
                ");

                return new ApiResponse<object>
                {
                    status = true,
                    data = BankList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetPartyListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var partyList = await _dbHelper.GetJsonDataAsync(@$" select CODE,NAME,ADD1,ADD2,ADD3,CITY_CODE,GSTIN,PINCODE, MOBILE
               from SUBGROUP_MAST where COMP_CODE={companyCd} and ACTIVE=1 order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = partyList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetCostCenterCodeListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var costList = await _dbHelper.GetJsonDataAsync(@$" select CODE, NAME from COSTCENTER_MAST where COMP_CODE={companyCd}  order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = costList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetColorListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var colorList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from COLOR_MAST where COMP_CODE={companyCd} order by NAME");
                return new ApiResponse<object>
                {
                    status = true,
                    data = colorList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetItemSizeMastListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var sizeList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from ITEMSIZE_MAST where COMP_CODE={companyCd} order by NAME");
                return new ApiResponse<object>
                {
                    status = true,
                    data = sizeList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetItemCatMastListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var itemCatList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from ITEMCAT_MAST where COMP_CODE={companyCd} order by NAME");
                return new ApiResponse<object>
                {
                    status = true,
                    data = itemCatList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetMeshListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var meshList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from MESHCONV_MAST where COMP_CODE={companyCd} order by NAME");
                return new ApiResponse<object>
                {
                    status = true,
                    data = meshList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetSaudaNoListAsync(string Vtype)
        {
            try
            {
                var userSessionDt = _globalValue.GetGlobalVariables();
                var companyCd = userSessionDt.PubCompCode;
                var yearCd = userSessionDt.PubFYearCode;
                var saudaList = await _dbHelper.GetJsonDataAsync(@$" select DOC_ID, V_NO from SAUDA where COMP_CODE={companyCd} and YEAR_CODE={yearCd} and BRANCH_CODE=1 and V_TYPE='{Vtype}' order by DOC_ID ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = saudaList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetTenaCityListAsync()
        {
            try
            {                 
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;               
                var dataList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from TENACITY_MAST where COMP_CODE={companyCd} order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = dataList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetCityListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from CITY_MAST order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = dataList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetStateListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from STATE_MAST order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = dataList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }
        public async Task<ApiResponse<object>> GetCountryListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from COUNTRY_MAST order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = dataList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }

        public async Task<ApiResponse<object>> GetPaymentTermListAsync()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;           
                var dataList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from PAYTERM_MAST where COMP_CODE={companyCd} order by NAME ");
                return new ApiResponse<object>
                {
                    status = true,
                    data = dataList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    status = false,
                    message = "data load failed"
                };
            }
        }


    }
}

 