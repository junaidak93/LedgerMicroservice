using System.Diagnostics;
using System.Text;
using Ledger.API.Services;
using Ledger.API.Helpers;

namespace Ledger.API.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            responseBody.Position = 0;

            // Log request/response for audit purposes
            var requestPath = context.Request.Path.Value ?? "";
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var userId = ClaimsHelper.GetUserId(context.User);
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Only log significant operations (not health checks, etc.)
            if (!requestPath.Contains("/health") && !requestPath.Contains("/swagger"))
            {
                _logger.LogInformation(
                    "Request: {Method} {Path} | Status: {StatusCode} | UserId: {UserId} | Duration: {Duration}ms",
                    method, requestPath, statusCode, userId, stopwatch.ElapsedMilliseconds);
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

