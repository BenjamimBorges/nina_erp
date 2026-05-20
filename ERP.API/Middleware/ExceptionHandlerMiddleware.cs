using System.Net;
using System.Text.Json;

namespace ERP.API.Middleware
{
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
                _logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                ArgumentException or ArgumentNullException => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Acesso não autorizado."),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Recurso não encontrado."),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente mais tarde.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                statusCode = (int)statusCode,
                message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
