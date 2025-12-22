using Microsoft.Extensions.Primitives;

namespace UltimateTicTacToe.API.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Ensure response always contains the correlation id.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out StringValues values))
        {
            var provided = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(provided))
                return provided!;
        }

        var id = Guid.NewGuid().ToString("N");
        context.Request.Headers[HeaderName] = id;
        return id;
    }
}

