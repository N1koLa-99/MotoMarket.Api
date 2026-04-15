using MotoMarket.Api.Models.Entities;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<PendingListingAction?> GetPendingActionByIdAsync(long pendingActionId);
    Task<PendingListingAction?> GetPendingActionByProviderOrderIdAsync(string providerOrderId);

    Task UpdatePendingActionProviderOrderIdAsync(long pendingActionId, string providerOrderId);
    Task MarkPendingActionCompletedAsync(long pendingActionId, string? providerPaymentId);
    Task MarkPendingActionCancelledAsync(long pendingActionId);
    Task MarkPendingActionFailedAsync(long pendingActionId);

    Task<List<PendingListingAction>> GetExpiredPendingActionsAsync(int take = 100);
}