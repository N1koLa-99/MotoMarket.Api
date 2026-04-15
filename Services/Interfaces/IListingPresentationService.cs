using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IListingPresentationService
{
    Task<PublicListingSearchResponse> SearchPublicAsync(PublicListingSearchRequest request, long? viewerUserId = null);
    Task<PublicListingDetailsResponse> GetPublicByIdAsync(long listingId, long? viewerUserId = null, bool incrementViewCount = true);
}