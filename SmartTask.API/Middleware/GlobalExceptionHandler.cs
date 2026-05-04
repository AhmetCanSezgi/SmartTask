using Microsoft.AspNetCore.Diagnostics;
using SmartTask.Application.Common.Exceptions;
using SmartTask.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace SmartTask.API.Middleware;

/// <summary>
/// Tüm exception'lar buraya düşer — controller'larda try/catch yok.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest, "Validation Error", ve.Errors),
            NotFoundException => (
                HttpStatusCode.NotFound, "Not Found", null),
            UnauthorizedDomainException => (
                HttpStatusCode.Forbidden, "Forbidden", null),
            DomainException => (
                HttpStatusCode.BadRequest, "Domain Error", null),
            _ => (HttpStatusCode.InternalServerError, "Server Error", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            title,
            status = (int)statusCode,
            detail = exception.Message,
            errors
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response), ct);

        return true;
    }
}
