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
    Task<AdminDocumentsResponse> GetDocumentsAsync(
        string? search,
        int? ownerId,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminDocumentItemDto?> GetDocumentByIdAsync(int id);
    Task<AdminQuizzesResponse> GetQuizzesAsync(
        string? search,
        int? ownerId,
        string? activityType,
        string? status,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminQuizItemDto?> GetQuizByIdAsync(int id);
}
