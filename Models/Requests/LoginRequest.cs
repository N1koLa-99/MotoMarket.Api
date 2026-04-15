using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Password { get; set; } = default!;
}