using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class RegisterCompanyRequest
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = default!;

    [Required, MaxLength(50)]
    public string CompanyVatNumber { get; set; } = default!;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [Required, MaxLength(30)]
    public string Phone { get; set; } = default!;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = default!;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = default!;

    [Required]
    public int CountryId { get; set; }

    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
}