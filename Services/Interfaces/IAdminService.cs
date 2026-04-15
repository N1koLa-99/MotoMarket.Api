using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IAdminService
{
    Task<PagedResultResponse<AdminUserListItemResponse>> GetUsersAsync(string? searchTerm, int page, int pageSize);
    Task<AdminUserDetailsResponse> GetUserDetailsAsync(long userId);
    Task<PagedResultResponse<ProfileListingCardResponse>> GetUserListingsAsync(long userId, int page, int pageSize);
    Task<PagedResultResponse<PaymentHistoryItemResponse>> GetPaymentsAsync(string? searchTerm, int page, int pageSize);
    Task<PagedResultResponse<AdminPendingActionResponse>> GetPendingActionsAsync(string? status, int page, int pageSize);
}