namespace MotoMarket.Api.Models.Responses;

public class AdminUserDetailsResponse
{
    public long Id { get; set; }
    public string RoleName { get; set; } = default!;
    public string AccountType { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? CompanyName { get; set; }
    public string? CompanyVatNumber { get; set; }
    public string? ContactPerson { get; set; }

    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public int CountryId { get; set; }
    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    public string? CountryName { get; set; }
    public string? RegionName { get; set; }
    public string? CityName { get; set; }

    public int PublishedListingsTotalCount { get; set; }
    public int ActiveListingsCount { get; set; }

    public int FavoritesCount { get; set; }
    public int PaymentsCount { get; set; }
    public decimal RevenueGeneratedEUR { get; set; }

    public int FreeUploadsRemainingNow { get; set; }
    public int OverFreeLimitCount { get; set; }

    public int PrivateFreeUsedCount { get; set; }
    public int CompanyStarterFreeUsedCount { get; set; }
    public int CompanyMonthlyFreeUsedCount { get; set; }
    public short? CompanyMonthlyQuotaYear { get; set; }
    public byte? CompanyMonthlyQuotaMonth { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
