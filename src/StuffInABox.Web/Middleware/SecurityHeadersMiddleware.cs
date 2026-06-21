namespace StuffInABox.Web.Middleware;

/// <summary>
/// Adds common security response headers. The CSP allows Google Fonts (the only
/// external resource the SPA loads), plus inline styles (the UI is styled inline),
/// blob/data images (photo previews), and same-origin XHR to the API. Icons are
/// bundled SVG components, so no icon CDN is needed.
/// </summary>
public static class SecurityHeadersMiddleware
{
    // SHA-256 of the inline theme-init script in ClientApp/index.html (runs before
    // first paint to set data-theme, avoiding a light→dark flash). If that script
    // changes, recompute this hash — keeping script-src otherwise free of 'unsafe-inline'.
    private const string ThemeScriptHash = "'sha256-44oUjcpwRxvRj5LHU+Rw2hWiyR5rFJQOISEkuLZAE4U='";

    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "img-src 'self' data: blob:; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "script-src 'self' " + ThemeScriptHash + "; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self' https://accounts.google.com https://appleid.apple.com";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            var headers = ctx.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            await next();
        });
}
