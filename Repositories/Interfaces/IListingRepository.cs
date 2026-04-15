using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface IListingRepository
{
    Task<User?> GetUserByIdAsync(long userId);

    Task<Listing?> GetListingByIdAsync(long listingId);
    Task<List<ListingPhoto>> GetListingPhotosAsync(long listingId);
    Task<List<string>> GetListingBlobNamesAsync(long listingId);

    Task<long> InsertListingAsync(Listing listing);
    Task UpdateListingAsync(long listingId, UpdateListingRequest request, decimal priceEur);
    Task ReplaceListingPhotosAsync(long listingId, List<ListingPhotoRequest> photos);

    Task UpdateListingPromotionAsync(long listingId, string promotionType, DateTime startAt, DateTime endAt);
    Task RefreshListingAsync(long listingId, DateTime refreshAt);
    Task IncrementViewCountAsync(long listingId);

    Task DeleteListingAsync(long listingId);

    Task IncreaseUserPublishedCountersAsync(long userId, string accountType);

    Task<long> InsertPaymentAsync(long userId, long? listingId, string serviceType, decimal amountEUR, string status, string? note);
    Task<long> InsertPendingActionAsync(PendingListingAction action);

    Task<int> CountPublicSearchAsync(PublicListingSearchRequest request);
    Task<List<PublicListingCardResponse>> SearchPublicAsync(PublicListingSearchRequest request);
    Task<PublicListingDetailsResponse?> GetPublicDetailsAsync(long listingId);
}