using System.Net;
using Finance.MarketDataService.API.Models;

namespace Finance.MarketDataService.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            _logger.LogError(ex, "Unhandled exception");
            await context.Response.WriteAsJsonAsync(new CustomValidationProblemDetails
            {
                Title  = ex.Message,
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = ex.StackTrace
            });
        }
    }
}
