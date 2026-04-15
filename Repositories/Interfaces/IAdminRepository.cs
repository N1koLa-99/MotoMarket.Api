using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface IAdminRepository
{
    Task<User?> GetUserByIdAsync(long userId);

    Task<int> CountUsersAsync(string? searchTerm);
    Task<List<AdminUserListItemResponse>> GetUsersAsync(string? searchTerm, int page, int pageSize);

    Task<AdminUserDetailsResponse?> GetUserDetailsAsync(long userId);

    Task<int> CountUserListingsAsync(long userId);
    Task<List<ProfileListingCardResponse>> GetUserListingsAsync(long userId, int page, int pageSize);

    Task<int> CountPaymentsAsync(string? searchTerm);
    Task<List<PaymentHistoryItemResponse>> GetPaymentsAsync(string? searchTerm, int page, int pageSize);

    Task<int> CountPendingActionsAsync(string? status);
    Task<List<AdminPendingActionResponse>> GetPendingActionsAsync(string? status, int page, int pageSize);
}
