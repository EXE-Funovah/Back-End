using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminService
{
    Task<AdminOverviewResponse> GetOverviewAsync(string range);
    Task<AdminRevenueResponse> GetRevenueAsync(string range);
    Task<AdminUsersResponse> GetUsersAsync(
        string? search,
        string? role,
        string? subscription,
        int page,
        int pageSize);
    Task<AdminUserDetailResponse?> GetUserByIdAsync(int userId);
}
