using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class ResendEmailVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}