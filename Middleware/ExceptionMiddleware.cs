using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using TransactionManager.Exceptions;

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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError; // 500 by default
        string type = "about:blank"; // Default type when not specified
        string title = "Internal Server Error";
        string detail = exception.Message;
        string instance = context.Request.GetDisplayUrl();

        //todo: we probably may want to have inner exception messages in "errors" array
        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                title = "Invalid request";
                type = "https://example.com/probs/invalid-request";
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Forbidden;
                title = "Access denied";
                type = "https://example.com/probs/access-denied";
                break;

            case InsufficientAmountException:
                statusCode = HttpStatusCode.PaymentRequired;
                title = "Insufficient funds";
                type = "https://example.com/probs/insufficient-funds";
                break;

            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                title = "Resource not found";
                type = "https://example.com/probs/not-found";
                break;
            case DBConcurrencyException:
            case DbUpdateConcurrencyException:
                statusCode = HttpStatusCode.Conflict;
                title = "Client balance has been changed. Please repeat transaction";
                type = "https://example.com/probs/conflict";
                break;
            case NotLastTransactionException:
                statusCode = HttpStatusCode.Conflict;
                title = "The transaction cannot be backdated.";
                type = "https://example.com/probs/conflict";
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

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}