using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = default!;

    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = default!;
}