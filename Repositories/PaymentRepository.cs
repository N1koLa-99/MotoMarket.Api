using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PaymentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PendingListingAction?> GetPendingActionByIdAsync(long pendingActionId)
    {
        const string sql = """
SELECT TOP 1 *
FROM dbo.PendingListingActions
WHERE Id = @PendingActionId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PendingListingAction>(sql, new { PendingActionId = pendingActionId });
    }

    public async Task<PendingListingAction?> GetPendingActionByProviderOrderIdAsync(string providerOrderId)
    {
        const string sql = """
SELECT TOP 1 *
FROM dbo.PendingListingActions
WHERE ProviderOrderId = @ProviderOrderId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PendingListingAction>(sql, new { ProviderOrderId = providerOrderId });
    }

    public async Task UpdatePendingActionProviderOrderIdAsync(long pendingActionId, string providerOrderId)
    {
        const string sql = """
UPDATE dbo.PendingListingActions
SET ProviderOrderId = @ProviderOrderId
WHERE Id = @PendingActionId
  AND Status = 'PENDING';
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            PendingActionId = pendingActionId,
            ProviderOrderId = providerOrderId
        });
    }

    public async Task MarkPendingActionCompletedAsync(long pendingActionId, string? providerPaymentId)
    {
        const string sql = """
UPDATE dbo.PendingListingActions
SET
    Status = 'COMPLETED',
    CompletedAt = SYSUTCDATETIME(),
    ProviderPaymentId = @ProviderPaymentId
WHERE Id = @PendingActionId
  AND Status = 'PENDING';
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            PendingActionId = pendingActionId,
            ProviderPaymentId = providerPaymentId
        });
    }

    public async Task MarkPendingActionCancelledAsync(long pendingActionId)
    {
        const string sql = """
UPDATE dbo.PendingListingActions
SET Status = 'CANCELLED'
WHERE Id = @PendingActionId
  AND Status = 'PENDING';
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { PendingActionId = pendingActionId });
    }

    public async Task MarkPendingActionFailedAsync(long pendingActionId)
    {
        const string sql = """
UPDATE dbo.PendingListingActions
SET Status = 'FAILED'
WHERE Id = @PendingActionId
  AND Status = 'PENDING';
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { PendingActionId = pendingActionId });
    }

    public async Task<List<PendingListingAction>> GetExpiredPendingActionsAsync(int take = 100)
    {
        const string sql = """
SELECT TOP (@Take) *
FROM dbo.PendingListingActions
WHERE Status = 'PENDING'
  AND ExpiresAt IS NOT NULL
  AND ExpiresAt < SYSUTCDATETIME()
ORDER BY ExpiresAt ASC, Id ASC;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<PendingListingAction>(sql, new { Take = take });
        return result.ToList();
    }
}