namespace Genesis.AI.Api.Middleware;

public class ResponseHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseHeadersMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers.Append("Cache-Control", "no-store");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "deny");
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(self)");

        await _next(context);
    }
}
