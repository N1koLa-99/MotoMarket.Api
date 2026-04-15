using MotoMarket.Api.Models.Requests;

namespace MotoMarket.Api.Services.Interfaces;

public interface IListingValidationService
{
    Task ValidateCreateAsync(CreateListingRequest request);
    Task ValidateUpdateAsync(UpdateListingRequest request);
}