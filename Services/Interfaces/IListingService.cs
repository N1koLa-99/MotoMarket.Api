using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IListingService
{
    Task<ListingOperationResponse> CreateAsync(long userId, CreateListingRequest request);
    Task<ListingOperationResponse> UpdateAsync(long userId, long listingId, UpdateListingRequest request);
    Task<ListingOperationResponse> DeleteAsync(long userId, long listingId);
    Task<ListingOperationResponse> RefreshAsync(long userId, long listingId);
    Task<ListingOperationResponse> PromoteAsync(long userId, long listingId, PromoteListingRequest request);

    Task<ListingDetailsResponse> GetByIdAsync(long listingId);

    Task<PublicListingSearchResponse> SearchPublicAsync(PublicListingSearchRequest request);
    Task<PublicListingDetailsResponse> GetPublicByIdAsync(long listingId, bool incrementViewCount = true);
}