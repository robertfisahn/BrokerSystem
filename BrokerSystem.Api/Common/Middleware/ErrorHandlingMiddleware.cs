using System.Net;
using System.Text.Json;
using BrokerSystem.Api.Common.Exceptions;

namespace BrokerSystem.Api.Common.Middleware;

public class ErrorHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

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
        catch (ForbidException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.Forbidden, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong.");
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "Something went wrong.");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(result);
    }
}
