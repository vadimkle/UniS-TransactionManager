using System.ComponentModel.DataAnnotations;
using System.Net;
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
            // Log the error
            // ...

            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        bool isValidation = exception is ValidationException;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode =
            isValidation ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = $"{(isValidation ? "Bad request" : "Internal Server Error")}:{exception.Message}"
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}