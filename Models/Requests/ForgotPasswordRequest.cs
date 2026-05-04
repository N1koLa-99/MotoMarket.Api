using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}