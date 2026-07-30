using System.Net;
using System.Text.Json;
using TurkcellMeetingAssistant.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TurkcellMeetingAssistant.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong: {Message}", ex.Message);
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = exception switch
        {
            TurkcellMeetingAssistant.Application.Common.Exceptions.NotFoundException => HttpStatusCode.NotFound,
            TurkcellMeetingAssistant.Application.Common.Exceptions.BadRequestException => HttpStatusCode.BadRequest,
            TurkcellMeetingAssistant.Application.Common.Exceptions.UnauthorizedException => HttpStatusCode.Unauthorized,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            TurkcellMeetingAssistant.Application.Common.Exceptions.AnalysisAlreadyInProgressException => HttpStatusCode.Conflict,
            TurkcellMeetingAssistant.Application.Common.Exceptions.TranscriptNotFoundException => HttpStatusCode.BadRequest,
            TurkcellMeetingAssistant.Application.Common.Exceptions.TranscriptTooShortException => HttpStatusCode.BadRequest,
            TurkcellMeetingAssistant.Application.Common.Exceptions.AiResultValidationException => HttpStatusCode.UnprocessableEntity,
            _ => HttpStatusCode.InternalServerError
        };

        var message = exception.Message;

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(
            message: message,
            errors: new List<ApiError> { new ApiError { Field = "Server", ErrorMessage = exception.Message } }
        );

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
