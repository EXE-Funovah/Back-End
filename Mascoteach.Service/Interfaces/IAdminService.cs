using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminService
{
    Task<AdminOverviewResponse> GetOverviewAsync(string range);
    Task<AdminRevenueResponse> GetRevenueAsync(string range);
    Task<AdminAccountsResponse> GetAccountsAsync(string? search, string? tier, int page, int pageSize);
}
