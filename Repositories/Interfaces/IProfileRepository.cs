using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface IProfileRepository
{
    Task<User?> GetUserByIdAsync(long userId);

    Task<int> GetActiveListingsCountAsync(long userId);
    Task<int> GetFavoritesCountAsync(long userId);
    Task<int> GetPaidListingActionsCountAsync(long userId);
    Task<int> GetPaymentsCountAsync(long userId);
    Task<decimal> GetTotalPaidAmountAsync(long userId);

    Task<int> CountOwnListingsAsync(long userId);
    Task<List<ProfileListingCardResponse>> GetOwnListingsAsync(long userId, int page, int pageSize);

    Task<int> CountFavoriteListingsAsync(long userId);
    Task<List<ProfileListingCardResponse>> GetFavoriteListingsAsync(long userId, int page, int pageSize);

    Task<int> CountPaymentsAsync(long userId);
    Task<List<PaymentHistoryItemResponse>> GetPaymentsAsync(long userId, int page, int pageSize);

    Task<bool> IsFavoriteAsync(long userId, long listingId);
    Task AddFavoriteAsync(long userId, long listingId);
    Task RemoveFavoriteAsync(long userId, long listingId);
    Task<bool> ListingExistsAsync(long listingId);
}