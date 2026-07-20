using Kinxter.Auth;
using Kinxter.Auth.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var authOptions = AuthOptions.FromConfiguration(builder.Configuration);

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

if (!string.IsNullOrWhiteSpace(authOptions.PathBase))
{
    app.UsePathBase(authOptions.PathBase);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (AuthOptions options) => Results.Content(AuthHtml.Home(options), "text/html"))
    .WithName("GetAuthHome");

app.MapGet("/health", (AuthOptions options) => Results.Ok(new
{
    status = "ok",
    service = "Kinxter.Auth",
    realm = options.Realm,
    issuer = options.Issuer
}))
    .WithName("GetAuthHealth");

app.MapAccountEndpoints();
app.MapOpenIddictEndpoints();

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await app.ApplyAuthDatabaseAsync();
}

await app.RunAsync();
