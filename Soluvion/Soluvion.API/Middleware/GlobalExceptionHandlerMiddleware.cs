using System.Net;
using System.Text.Json;

namespace Soluvion.API.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Továbbengedjük a kérést a következő middleware-nek
                await _next(context);
            }
            catch (Exception ex)
            {
                // Ha bárhol az alkalmazásban kezeletlen kivétel történik, itt elkapjuk
                _logger.LogError(ex, "Kritikus szerver hiba történt: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var errorList = new List<string> { exception.Message };
            var inner = exception.InnerException;
            while (inner != null)
            {
                errorList.Add(inner.Message);
                inner = inner.InnerException;
            }

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Belső szerver hiba történt. Kérjük, próbálja újra később.",
                Detailed = string.Join(" --> ", errorList)
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}