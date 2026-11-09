namespace MotoMarket.Api.Models.Responses;

public class PublicUserProfileResponse
{
    public long UserId { get; set; }
    public string AccountType { get; set; } = default!;

    public string? DisplayName { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactPerson { get; set; }
    public string? CompanyDescription { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }

    public string? CountryName { get; set; }
    public string? RegionName { get; set; }
    public string? CityName { get; set; }

    public int ActiveListingsCount { get; set; }
    public DateTime MemberSince { get; set; }
}
