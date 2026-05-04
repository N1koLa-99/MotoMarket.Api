using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Repositories;

public class AccountCodeRepository : IAccountCodeRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AccountCodeRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InsertAsync(UserAccountCode code)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
INSERT INTO UserAccountCodes
(
    UserId,
    Purpose,
    CodeHash,
    ExpiresAtUtc,
    ConsumedAtUtc,
    AttemptCount,
    CreatedAtUtc,
    CreatedByIp
)
VALUES
(
    @UserId,
    @Purpose,
    @CodeHash,
    @ExpiresAtUtc,
    NULL,
    0,
    SYSUTCDATETIME(),
    @CreatedByIp
);";

        await connection.ExecuteAsync(sql, code);
    }

    public async Task<UserAccountCode?> GetLatestActiveCodeAsync(
        long userId,
        string purpose,
        DateTime nowUtc)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT TOP 1
    Id,
    UserId,
    Purpose,
    CodeHash,
    ExpiresAtUtc,
    ConsumedAtUtc,
    AttemptCount,
    CreatedAtUtc,
    CreatedByIp
FROM UserAccountCodes
WHERE UserId = @UserId
  AND Purpose = @Purpose
  AND ConsumedAtUtc IS NULL
  AND ExpiresAtUtc >= @NowUtc
ORDER BY Id DESC;";

        return await connection.QuerySingleOrDefaultAsync<UserAccountCode>(
            sql,
            new
            {
                UserId = userId,
                Purpose = purpose,
                NowUtc = nowUtc
            });
    }

    public async Task MarkConsumedAsync(long codeId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE UserAccountCodes
SET ConsumedAtUtc = SYSUTCDATETIME()
WHERE Id = @CodeId
  AND ConsumedAtUtc IS NULL;";

        await connection.ExecuteAsync(sql, new { CodeId = codeId });
    }

    public async Task IncreaseAttemptCountAsync(long codeId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE UserAccountCodes
SET AttemptCount = AttemptCount + 1
WHERE Id = @CodeId;";

        await connection.ExecuteAsync(sql, new { CodeId = codeId });
    }

    public async Task ConsumeActiveCodesAsync(long userId, string purpose)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE UserAccountCodes
SET ConsumedAtUtc = SYSUTCDATETIME()
WHERE UserId = @UserId
  AND Purpose = @Purpose
  AND ConsumedAtUtc IS NULL;";

        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            Purpose = purpose
        });
    }

    public async Task ConfirmUserEmailAsync(long userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE Users
SET EmailConfirmed = 1,
    EmailConfirmedAtUtc = SYSUTCDATETIME()
WHERE Id = @UserId;";

        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}