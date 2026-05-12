using System.Net;
using Finance.IdentityService.API.Models;
using Finance.IdentityService.Application.Exceptions;

namespace Finance.IdentityService.API.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var problem = new CustomValidationProblemDetails();

        switch (ex)
        {
            case BadRequestException bad:
                statusCode = HttpStatusCode.BadRequest;
                problem = new CustomValidationProblemDetails
                {
                    Title = bad.Message,
                    Status = (int)statusCode,
                    Type = nameof(BadRequestException),
                    Detail = bad.InnerException?.Message,
                    Errors = bad.ValidationErrors ?? new Dictionary<string, string[]>()
                };
                break;

            case NotFoundException notFound:
                statusCode = HttpStatusCode.NotFound;
                problem = new CustomValidationProblemDetails
                {
                    Title = notFound.Message,
                    Status = (int)statusCode,
                    Type = nameof(NotFoundException),
                    Detail = notFound.InnerException?.Message
                };
                break;

            default:
                problem = new CustomValidationProblemDetails
                {
                    Title = ex.Message,
                    Status = (int)statusCode,
                    Type = nameof(HttpStatusCode.InternalServerError),
                    Detail = ex.StackTrace
                };
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        _logger.LogError("Unhandled exception: {Title} | {Detail}", problem.Title, problem.Detail);
        await context.Response.WriteAsJsonAsync(problem);
    }
}
