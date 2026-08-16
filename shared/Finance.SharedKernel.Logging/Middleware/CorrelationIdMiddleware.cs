using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace Finance.SharedKernel.Logging.Middleware;

/// <summary>
/// Reads (or generates) a correlation id per request, echoes it back on the response,
/// and pushes it onto the Serilog LogContext + <see cref="CorrelationContext"/> so every
/// log line for this request — and any RabbitMQ/Service Bus message it triggers — carries it.
/// Also tags it onto the current OTel Activity (if telemetry is enabled) so it shows up as a
/// custom dimension on the matching Application Insights trace, for cross-referencing against
/// Seq — the id itself stays independent of the W3C trace id that actually stitches the trace.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !StringValues.IsNullOrEmpty(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("n");

        // Stamp the request too (not just the response) so a freshly generated id survives
        // being forwarded onward — e.g. the Gateway minting one that YARP then proxies downstream.
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        CorrelationContext.CorrelationId = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
