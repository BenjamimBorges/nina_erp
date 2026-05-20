using System.Net;
using System.Text.Json;
using FluentValidation;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
            _logger.LogError(ex, "Erro: {Message}", ex.Message);
            await HandleAsync(context, ex);
        }
    }

    private static Task HandleAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/json";

        var (status, message, errors) = ex switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, "Dados inválidos.",
                ve.Errors.Select(e => e.ErrorMessage)),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message,
                Enumerable.Empty<string>()),
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message,
                Enumerable.Empty<string>()),
            _ => (HttpStatusCode.InternalServerError, "Erro interno. Tente novamente.",
                Enumerable.Empty<string>())
        };

        ctx.Response.StatusCode = (int)status;
        var response = ApiResponse<object>.Fail(message, errors);
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
