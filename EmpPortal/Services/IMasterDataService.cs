using Microsoft.EntityFrameworkCore.Metadata.Internal;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Services
{
    public interface IMasterDataService
    {
        Task<ApiResponse<object>> GetBankMastAsync();
        Task<ApiResponse<object>> GetCityListAsync();
        Task<ApiResponse<object>> GetCostCenterCodeListAsync();
        Task<ApiResponse<object>> GetColorListAsync();
        Task<ApiResponse<object>> GetCountryListAsync();
        Task<ApiResponse<object>> GetDesignationMastAsync();
        Task<ApiResponse<object>> GetDenierMastAsync();
        Task<ApiResponse<object>> GetDocTypeAsync(string docType);
        Task<ApiResponse<object>> GetEmployeeMastAsync();
        Task<ApiResponse<object>> GetEmployeeDepartMastAsync();
        Task<ApiResponse<object>> GetHodMastAsync();
        Task<ApiResponse<object>> GetItemCatMastListAsync();
        Task<ApiResponse<object>> GetItemSizeMastListAsync();
        Task<ApiResponse<object>> GetItemListAsync();
        Task<ApiResponse<object>> GetItemDepartmentMastForProdAsync();
        Task<ApiResponse<object>> GetMeshListAsync();
        Task<ApiResponse<object>> GetMaxVNoAsync(string vType, string tableName);
        Task<ApiResponse<object>> GetPaymentTermListAsync();
        Task<ApiResponse<object>> GetPlaceListAsync();
        Task<ApiResponse<object>> GetPartyListAsync();
        Task<ApiResponse<object>> GetRawItemListAsync();
        Task<ApiResponse<object>> GetStrengthListAsync();
        Task<ApiResponse<object>> GetStatusMastAsync();
        Task<ApiResponse<object>> GetStateListAsync();
        Task<ApiResponse<object>> GetShiftMastAsync();
        Task<ApiResponse<object>> GetSaudaNoListAsync(string Vtype);
        Task<ApiResponse<object>> GetTenaCityListAsync();
        Task<ApiResponse<object>> GetUserListAsync();

    }
}
