using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class PromoteListingRequest
{
    [Required, MaxLength(10)]
    public string TargetPromotionType { get; set; } = default!; // TOP / VIP
}