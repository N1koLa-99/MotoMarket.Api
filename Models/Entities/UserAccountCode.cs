namespace MotoMarket.Api.Models.Entities;

public class UserAccountCode
{
    public long Id { get; set; }
    public long UserId { get; set; }

    public string Purpose { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
}