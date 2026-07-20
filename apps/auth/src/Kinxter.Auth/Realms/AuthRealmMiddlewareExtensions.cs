namespace Kinxter.Auth;

internal static class AuthRealmHttpContextExtensions
{
    private const string RealmOptionsItemKey = "__KinxterAuthRealmOptions";

    public static AuthOptions? GetAuthRealmOptions(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(RealmOptionsItemKey, out var value)
            ? value as AuthOptions
            : null;
    }

    public static void SetAuthRealmOptions(this HttpContext context, AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        context.Items[RealmOptionsItemKey] = options;
    }
}

internal static class AuthRealmMiddlewareExtensions
{
    public static IApplicationBuilder UseKinxterAuthRealms(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var serverOptions = context.RequestServices.GetRequiredService<AuthServerOptions>();

            if (serverOptions.TryFindByPath(context.Request.Path, out var realmOptions, out var remainingPath))
            {
                var originalPath = context.Request.Path;
                var originalPathBase = context.Request.PathBase;

                context.SetAuthRealmOptions(realmOptions);
                context.Request.PathBase = originalPathBase.Add(new PathString(realmOptions.PathBase));
                context.Request.Path = remainingPath.HasValue ? remainingPath : "/";

                try
                {
                    await next(context);
                }
                finally
                {
                    context.Request.Path = originalPath;
                    context.Request.PathBase = originalPathBase;
                }

                return;
            }

            if (RequiresRealm(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;

                return;
            }

            await next(context);
        });
    }

    private static bool RequiresRealm(PathString path)
    {
        return path.StartsWithSegments("/account") ||
            path.StartsWithSegments("/connect") ||
            path.StartsWithSegments("/.well-known");
    }
}
