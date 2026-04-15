using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileDashboardResponse> GetDashboardAsync(long userId);

    Task<PagedResultResponse<ProfileListingCardResponse>> GetMyListingsAsync(long userId, int page, int pageSize);
    Task<PagedResultResponse<ProfileListingCardResponse>> GetMyFavoritesAsync(long userId, int page, int pageSize);
    Task<PagedResultResponse<PaymentHistoryItemResponse>> GetMyPaymentsAsync(long userId, int page, int pageSize);

    Task AddFavoriteAsync(long userId, long listingId);
    Task RemoveFavoriteAsync(long userId, long listingId);
    Task<bool> IsFavoriteAsync(long userId, long listingId);
}