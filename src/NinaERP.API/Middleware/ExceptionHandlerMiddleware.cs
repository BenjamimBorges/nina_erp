using System.Net;
using System.Text.Json;
using FluentValidation;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Middleware;
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro: {Msg}", ex.Message);
            ctx.Response.ContentType = "application/json";
            var (status, msg, errors) = ex switch
            {
                ValidationException ve => (HttpStatusCode.BadRequest, "Dados inválidos.",
                    ve.Errors.Select(e => e.ErrorMessage)),
                NotFoundException => (HttpStatusCode.NotFound, ex.Message, Enumerable.Empty<string>()),
                BusinessException => (HttpStatusCode.UnprocessableEntity, ex.Message, Enumerable.Empty<string>()),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message, Enumerable.Empty<string>()),
                _ => (HttpStatusCode.InternalServerError, "Erro interno.", Enumerable.Empty<string>())
            };
            ctx.Response.StatusCode = (int)status;
            var response = ApiResponse<object>.Fail(msg, errors);
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
