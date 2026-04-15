namespace MotoMarket.Api.Models.Responses;

public class ProfileDashboardResponse
{
    public long UserId { get; set; }
    public string RoleName { get; set; } = default!;
    public string AccountType { get; set; } = default!;

    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public int PublishedListingsTotalCount { get; set; }
    public int ActiveListingsCount { get; set; }
    public int FavoritesCount { get; set; }

    public int PaidListingActionsCount { get; set; }
    public int TotalPaymentsCount { get; set; }
    public decimal TotalPaidAmountEUR { get; set; }

    public int FreeUploadsRemainingNow { get; set; }
    public int OverFreeLimitCount { get; set; }

    public int PrivateFreeLimitLifetime { get; set; }
    public int PrivateFreeUsedLifetime { get; set; }

    public int CompanyStarterFreeLimitLifetime { get; set; }
    public int CompanyStarterFreeUsedLifetime { get; set; }

    public int CompanyMonthlyFreeLimit { get; set; }
    public int CompanyMonthlyFreeUsedCurrentMonth { get; set; }
    public int CompanyMonthlyFreeRemainingCurrentMonth { get; set; }

    public bool IsInMonthlyCompanyQuotaMode { get; set; }
}