using System.Net;
using Finance.SentimentService.API.Models;

namespace Finance.SentimentService.API.Middleware;

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
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var problem = new CustomValidationProblemDetails
            {
                Title = ex.Message,
                Status = (int)HttpStatusCode.InternalServerError,
                Type = nameof(HttpStatusCode.InternalServerError),
                Detail = ex.StackTrace
            };
            _logger.LogError("Unhandled exception: {Title}", problem.Title);
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
