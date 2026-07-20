using Kinxter.Auth;
using Kinxter.Auth.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var authOptions = AuthServerOptions.FromConfiguration(builder.Configuration);

builder.Services.AddKinxterAuth(
    builder.Configuration,
    builder.Environment,
    authOptions);

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
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (
        HttpContext context,
        AuthServerOptions serverOptions,
        AuthPageRenderer renderer) =>
    {
        return renderer.HomeAsync(context, serverOptions);
    })
    .WithName("GetAuthHome");

app.MapGet("/health", (HttpContext context, AuthServerOptions serverOptions) =>
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
                realms = serverOptions.Realms.Select(realm => realm.Realm).ToArray()
            });
    })
    .WithName("GetAuthHealth");

app.MapAccountEndpoints();
app.MapOpenIddictEndpoints();

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await app.ApplyAuthDatabaseAsync();
}

await app.RunAsync();
