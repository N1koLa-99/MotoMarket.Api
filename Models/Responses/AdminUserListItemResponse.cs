namespace MotoMarket.Api.Models.Responses;

public class AdminUserListItemResponse
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

    public int PublishedListingsTotalCount { get; set; }
    public int ActiveListingsCount { get; set; }

    public int PaymentsCount { get; set; }
    public decimal RevenueGeneratedEUR { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}