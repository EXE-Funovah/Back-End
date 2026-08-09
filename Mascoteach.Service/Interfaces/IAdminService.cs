using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminService
{
    Task<AdminOverviewResponse> GetOverviewAsync(string range);
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
    Task<AdminSessionsResponse> GetSessionsAsync(
        string? search,
        int? teacherId,
        int? templateId,
        string? status,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminSessionItemDto?> GetSessionByIdAsync(int id);
    Task<AdminSessionParticipantsResponse?> GetSessionParticipantsAsync(
        int sessionId,
        string? search,
        string deletion,
        int page,
        int pageSize);
    Task<AdminPaymentOrdersResponse> GetBillingOrdersAsync(
        string? search,
        int? userId,
        string? status,
        string? plan,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminPaymentOrderItemDto?> GetBillingOrderByIdAsync(int id);
    Task<AdminRevenueExportResult> ExportBillingRevenueAsync(
        DateTime? from,
        DateTime? to,
        string? plan);
    Task<AdminWebhookEventsResponse> GetBillingWebhookEventsAsync(
        string? search,
        bool? processed,
        bool? hasError,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
}
