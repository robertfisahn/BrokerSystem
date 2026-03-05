using System.Net;
using System.Text.Json;
using BrokerSystem.Api.Common.Exceptions;

namespace BrokerSystem.Api.Common.Middleware;

public class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (BadRequestException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, "Validation failed", errors);
        }
        catch (ForbidException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.Forbidden, ex.Message);
        }
        catch (Exception ex)
        {
            var env = context.RequestServices.GetService<IHostEnvironment>();
            var message = (env?.IsDevelopment() == true || env?.IsEnvironment("IntegrationTest") == true) 
                ? ex.Message 
                : "Something went wrong.";

            _logger.LogError(ex, message);
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, message);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message, object? details = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new { error = message, details });
        await context.Response.WriteAsync(result);
    }
}
