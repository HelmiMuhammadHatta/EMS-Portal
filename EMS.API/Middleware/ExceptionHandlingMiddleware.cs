using System.Net;
using System.Text.Json;
using EMS.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EMS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = HttpStatusCode.InternalServerError;
        var message = exception.Message;
        
        switch (exception)
        {
            case EMS.Domain.Common.Exceptions.NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                break;
            case EMS.Domain.Common.Exceptions.BadRequestException:
                statusCode = HttpStatusCode.BadRequest;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailureResponse(message, new List<string> { exception.Message });
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return context.Response.WriteAsync(jsonResponse);
    }
}
