namespace MotoMarket.Api.Models.Entities;

public class User
{
    public long Id { get; set; }
    public string RoleName { get; set; } = "USER";
    public string AccountType { get; set; } = default!; // PRIVATE / COMPANY

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? CompanyName { get; set; }
    public string? CompanyVatNumber { get; set; }
    public string? ContactPerson { get; set; }

    public string Phone { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    public int CountryId { get; set; }
    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    public string? LogoUrl { get; set; }

    public int PublishedListingsTotalCount { get; set; }
    public int PrivateFreeUsedCount { get; set; }
    public int CompanyStarterFreeUsedCount { get; set; }
    public int CompanyMonthlyFreeUsedCount { get; set; }
    public short? CompanyMonthlyQuotaYear { get; set; }
    public byte? CompanyMonthlyQuotaMonth { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
    public DateTime? PrivacyPolicyAcceptedAtUtc { get; set; }
}