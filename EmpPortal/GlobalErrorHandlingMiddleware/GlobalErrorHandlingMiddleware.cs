using System.Net;
using System.Text.Json;

namespace travelexpensemanagement.GlobalErrorHandlingMiddleware
{
    public class GlobalErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

        public GlobalErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                // 🔹 HANDLE HTTP STATUS CODES (NO EXCEPTION)
                if (context.Response.StatusCode >= 400)
                {
                    await HandleStatusCode(context);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                await HandleException(context, ex);
            }
        }

        private async Task HandleException(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "Internal server error",
                detail = ex.Message // remove in production
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        private async Task HandleStatusCode(HttpContext context)
        {
            int code = context.Response.StatusCode;

            if (context.Response.HasStarted)
                return;

            // 👉 MVC PAGE REDIRECT
            context.Response.Redirect(
                $"/AccessedError/Index?code={code}&message={GetMessage(code)}"
            );

            await Task.CompletedTask;
        }

        private static string GetMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad request",
                401 => "Your session has expired. Please login again.",
                403 => "You are not authorized to access this page.",
                404 => "The page you are looking for does not exist.",
                429 => "Please try again after 2 minutes",
                500 => "Internal server error. Please contact administrator.",
                _ => "Unexpected error occurred."
            };
        }


    }
}

