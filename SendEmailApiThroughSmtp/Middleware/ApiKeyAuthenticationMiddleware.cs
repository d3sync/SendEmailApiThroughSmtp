namespace SendEmailApiThroughSmtp.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
        private const string API_KEY_HEADER = "X-API-Key";

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            // Skip authentication for Swagger UI and Swagger JSON endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.StartsWith("/swagger"))
            {
                await _next(context);
                return;
            }

            // Check if the request is for an API endpoint
            if (!path.StartsWith("/api"))
            {
                await _next(context);
                return;
            }

            // Validate API key
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                _logger.LogWarning("API key missing from request to {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "API key is missing" });
                return;
            }

            var configuredApiKey = configuration["ApiKey"];
            if (string.IsNullOrEmpty(configuredApiKey))
            {
                _logger.LogError("API key not configured in appsettings.json");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Server configuration error" });
                return;
            }

            if (!configuredApiKey.Equals(extractedApiKey))
            {
                _logger.LogWarning("Invalid API key provided for request to {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
                return;
            }

            _logger.LogDebug("API key validated successfully for request to {Path}", path);
            await _next(context);
        }
    }
}
