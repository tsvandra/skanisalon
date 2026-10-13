using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Soluvion.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

            var (statusCode, errorCode) = exception switch
            {
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, 
                    string.IsNullOrWhiteSpace(exception.Message) ? "UNAUTHORIZED" : exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND"),
                ArgumentException => (StatusCodes.Status400BadRequest, 
                    string.IsNullOrWhiteSpace(exception.Message) ? "BAD_REQUEST" : exception.Message),
                InvalidOperationException => (StatusCodes.Status400BadRequest, 
                    string.IsNullOrWhiteSpace(exception.Message) ? "BAD_REQUEST" : exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "SERVER_ERROR")
            };

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = errorCode,
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions["errorCode"] = errorCode;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}

