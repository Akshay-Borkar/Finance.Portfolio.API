using System.Net;
using Finance.SharedKernel.Auth.Exceptions;
using Finance.SharedKernel.Auth.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Finance.SharedKernel.Auth.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
                    Title = "Bad Request",
                    Status = (int)statusCode,
                    Type = nameof(BadRequestException),
                    Detail = bad.Message,
                    Errors = bad.ValidationErrors
                };
                break;

            case NotFoundException notFound:
                statusCode = HttpStatusCode.NotFound;
                problem = new CustomValidationProblemDetails
                {
                    Title = "Not Found",
                    Status = (int)statusCode,
                    Type = nameof(NotFoundException),
                    Detail = notFound.Message
                };
                break;

            default:
                _logger.LogError(ex, "Unhandled exception");
                problem = new CustomValidationProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = (int)statusCode,
                    Type = nameof(HttpStatusCode.InternalServerError),
                    Detail = "An unexpected error occurred."
                };
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
