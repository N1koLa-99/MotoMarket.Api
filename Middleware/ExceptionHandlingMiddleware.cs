using System.Net;
using System.Text.Json;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized access. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
                context.TraceIdentifier,
                context.Request.Path,
                context.Request.Method);

            await WriteError(
                context,
                HttpStatusCode.Unauthorized,
                "unauthorized",
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Business validation error. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
                context.TraceIdentifier,
                context.Request.Path,
                context.Request.Method);

            await WriteError(
                context,
                HttpStatusCode.BadRequest,
                "business_rule_error",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled server error. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
                context.TraceIdentifier,
                context.Request.Path,
                context.Request.Method);

            await WriteError(
                context,
                HttpStatusCode.InternalServerError,
                "internal_server_error",
                "Възникна вътрешна грешка.");
        }
    }

    private static async Task WriteError(
        HttpContext context,
        HttpStatusCode statusCode,
        string code,
        string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)statusCode;

        var payload = new ApiErrorResponse
        {
            Error = message,
            Code = code,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }
}