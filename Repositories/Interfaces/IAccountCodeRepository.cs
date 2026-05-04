using MotoMarket.Api.Models.Entities;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface IAccountCodeRepository
{
    Task InsertAsync(UserAccountCode code);

    Task<UserAccountCode?> GetLatestActiveCodeAsync(
        long userId,
        string purpose,
        DateTime nowUtc);

    Task MarkConsumedAsync(long codeId);

    Task IncreaseAttemptCountAsync(long codeId);

    Task ConsumeActiveCodesAsync(long userId, string purpose);

    Task ConfirmUserEmailAsync(long userId);
}