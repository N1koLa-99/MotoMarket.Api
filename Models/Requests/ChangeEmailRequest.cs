using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class ChangeEmailRequest
{
    [Required]
    public string CurrentPassword { get; set; } = default!;

    [Required, EmailAddress, MaxLength(200)]
    public string NewEmail { get; set; } = default!;
}