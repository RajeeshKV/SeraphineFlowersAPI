using System.Net;
using Microsoft.EntityFrameworkCore;

namespace SeraphineFlowers.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
                ? value?.ToString()
                : context.TraceIdentifier;

            var statusCode = exception switch
            {
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                DbUpdateConcurrencyException => HttpStatusCode.Conflict,
                InvalidOperationException => HttpStatusCode.Conflict,
                ArgumentException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            if ((int)statusCode >= 500)
            {
                logger.LogError(exception, "Request failed. CorrelationId: {CorrelationId}", correlationId);
            }
            else
            {
                logger.LogWarning("Request failed with {StatusCode}. CorrelationId: {CorrelationId}. Message: {Message}", (int)statusCode, correlationId, exception.Message);
            }

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                correlationId,
                status = context.Response.StatusCode,
                error = statusCode.ToString(),
                message = statusCode == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : exception.Message
            });
        }
    }
}
