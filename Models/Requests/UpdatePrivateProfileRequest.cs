using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class UpdatePrivateProfileRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = default!;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = default!;

    [Required]
    public int CountryId { get; set; }

    public int? RegionId { get; set; }

    public int? CityId { get; set; }
}