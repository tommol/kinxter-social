namespace Kinxter.Auth.Administration;

internal static class AuthAdminApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAuthAdminSecurityHeaders(
        this IApplicationBuilder app,
        AuthAdminOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(options.PathBase))
            {
                context.Response.Headers["Cache-Control"] = "no-store";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; " +
                    "frame-ancestors 'none'; base-uri 'none'";
            }

            await next(context);
        });
    }
}
