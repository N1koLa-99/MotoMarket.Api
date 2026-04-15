namespace MotoMarket.Api.Models.Responses;

public class UserProfileResponse
{
    public long Id { get; set; }
    public string RoleName { get; set; } = default!;
    public string AccountType { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? CompanyName { get; set; }
    public string? CompanyVatNumber { get; set; }
    public string? ContactPerson { get; set; }

    public string Phone { get; set; } = default!;
    public string Email { get; set; } = default!;

    public int CountryId { get; set; }
    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    public string? LogoUrl { get; set; }

    public int PublishedListingsTotalCount { get; set; }
    public int PrivateFreeUsedCount { get; set; }
    public int CompanyStarterFreeUsedCount { get; set; }
    public int CompanyMonthlyFreeUsedCount { get; set; }
}