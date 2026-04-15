using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Services.Interfaces;
using System.Text;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly MyPosOptions _myPosOptions;

    public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _myPosOptions = configuration.GetSection(MyPosOptions.SectionName).Get<MyPosOptions>()
            ?? throw new InvalidOperationException("Missing MyPos configuration.");
    }

    [Authorize]
    [HttpPost("mypos/start/{pendingActionId:long}")]
    public async Task<IActionResult> StartMyPosCheckout(long pendingActionId)
    {
        var userId = User.GetUserId();
        var result = await _paymentService.StartMyPosCheckoutAsync(userId, pendingActionId);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("mypos/notify")]
    public async Task<IActionResult> MyPosNotify()
    {
        var form = await Request.ReadFormAsync();
        var orderedFields = form
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))
            .ToList();

        await _paymentService.HandleMyPosNotifyAsync(orderedFields);

        return Content("OK", "text/plain", Encoding.UTF8);
    }

    [AllowAnonymous]
    [HttpGet("mypos/ok")]
    [HttpPost("mypos/ok")]
    public async Task<IActionResult> MyPosOk()
    {
        string? orderId = null;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            orderId = form["OrderID"].ToString();
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            orderId = Request.Query["OrderID"].ToString();
        }

        if (!string.IsNullOrWhiteSpace(_myPosOptions.FrontendSuccessUrl))
        {
            var redirectUrl = $"{_myPosOptions.FrontendSuccessUrl}?orderId={Uri.EscapeDataString(orderId ?? string.Empty)}";
            return Redirect(redirectUrl);
        }

        return Content("Плащането е обработено. Потвърждението идва от notify callback.", "text/plain", Encoding.UTF8);
    }

    [AllowAnonymous]
    [HttpGet("mypos/cancel")]
    [HttpPost("mypos/cancel")]
    public async Task<IActionResult> MyPosCancel()
    {
        IReadOnlyList<KeyValuePair<string, string>> orderedFields;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            orderedFields = form
                .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))
                .ToList();
        }
        else
        {
            orderedFields = Request.Query
                .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))
                .ToList();
        }

        await _paymentService.HandleMyPosCancelAsync(orderedFields);

        if (!string.IsNullOrWhiteSpace(_myPosOptions.FrontendCancelUrl))
            return Redirect(_myPosOptions.FrontendCancelUrl);

        return Content("Плащането е отменено.", "text/plain", Encoding.UTF8);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost("cleanup-expired-pending")]
    public async Task<IActionResult> CleanupExpiredPending([FromQuery] int take = 100)
    {
        if (take <= 0 || take > 1000)
            throw new InvalidOperationException("Невалиден take.");

        var cleaned = await _paymentService.CleanupExpiredPendingActionsAsync(take);

        return Ok(new
        {
            success = true,
            cleaned
        });
    }
}