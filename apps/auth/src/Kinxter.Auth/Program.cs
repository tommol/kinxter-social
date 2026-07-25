using Kinxter.Auth;
using Kinxter.Auth.Administration;
using Kinxter.Auth.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var authOptions = AuthServerOptions.FromConfiguration(builder.Configuration);
var authAdminOptions = AuthAdminOptions.FromConfiguration(builder.Configuration);

builder.Services.AddKinxterAuth(
    builder.Configuration,
    builder.Environment,
    authOptions,
    authAdminOptions);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(authOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseKinxterAuthRealms();
app.UseAuthAdminSecurityHeaders(authAdminOptions);
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (
        HttpContext context,
        AuthRealmRegistry realmRegistry,
        AuthPageRenderer renderer) =>
    {
        return renderer.HomeAsync(context, realmRegistry);
    })
    .WithName("GetAuthHome");

app.MapGet("/health", (HttpContext context, AuthRealmRegistry realmRegistry) =>
    {
        var realmOptions = context.GetAuthRealmOptions();

        return realmOptions is not null
            ? Results.Ok(new
            {
                status = "ok",
                service = "Kinxter.Auth",
                realm = realmOptions.Realm,
                issuer = realmOptions.Issuer
            })
            : Results.Ok(new
            {
                status = "ok",
                service = "Kinxter.Auth",
                realms = realmRegistry.Realms.Select(realm => realm.Realm).ToArray()
            });
    })
    .WithName("GetAuthHealth");

app.MapAccountEndpoints();
app.MapOpenIddictEndpoints();
app.MapAuthAdminEndpoints(authAdminOptions);

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await app.ApplyAuthDatabaseAsync();
}

await app.RunAsync();
