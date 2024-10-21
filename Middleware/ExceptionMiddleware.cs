using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace TransactionManager.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // todo - add logging

            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError; // 500 by default
        string type = "about:blank"; // Default type when not specified
        string title = "Internal Server Error";
        string detail = exception.Message;
        string instance = context.Request?.GetDisplayUrl() ?? string.Empty;

        switch (exception)
        {
            case ArgumentNullException _:
            case ArgumentException _:
                statusCode = HttpStatusCode.BadRequest; // 400
                title = "Invalid request";
                type = "https://example.com/probs/invalid-request";
                break;

            case UnauthorizedAccessException _:
                statusCode = HttpStatusCode.Forbidden; // 403
                title = "Access denied";
                type = "https://example.com/probs/access-denied";
                break;

            case InvalidOperationException _ when exception.Message.Contains("Insufficient funds"):
                statusCode = HttpStatusCode.PaymentRequired; // 402
                title = "Insufficient funds";
                type = "https://example.com/probs/insufficient-funds";
                break;

            case KeyNotFoundException _:
                statusCode = HttpStatusCode.NotFound; // 404
                title = "Resource not found";
                type = "https://example.com/probs/not-found";
                break;

            default:
                // Keep default 500 error with "about:blank" type
                break;
        }

        var problemDetails = new
        {
            type,
            title,
            status = (int)statusCode,
            detail,
            instance
        };

        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsJsonAsync(JsonSerializer.Serialize(problemDetails));
    }
}