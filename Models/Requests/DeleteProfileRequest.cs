using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class DeleteProfileRequest
{
    [Required]
    public string CurrentPassword { get; set; } = default!;
}