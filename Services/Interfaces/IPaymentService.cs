using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IPaymentService
{
    Task<MyPosCheckoutStartResponse> StartMyPosCheckoutAsync(long userId, long pendingActionId);
    Task<MyPosCallbackResultResponse> HandleMyPosNotifyAsync(IReadOnlyList<KeyValuePair<string, string>> formFields);
    Task<MyPosCallbackResultResponse> HandleMyPosCancelAsync(IReadOnlyList<KeyValuePair<string, string>> formFields);
    Task<int> CleanupExpiredPendingActionsAsync(int take = 100);
}