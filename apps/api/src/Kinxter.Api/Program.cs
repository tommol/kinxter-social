using Kinxter.Accounts;
using Kinxter.Api;
using Kinxter.Api.Authentication;
using Kinxter.Api.Contracts.Dtos;
using Kinxter.Profiles;
using Kinxter.Shared.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddAccountsModule(builder.Configuration);
builder.Services.AddProfilesModule(builder.Configuration);
builder.Services.AddKinxterApiAuthentication(builder.Configuration);
builder.Services.AddOpenApi("v1");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Kinxter API Reference");
    options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
});

app.MapGet("/health", () => Results.Ok(new HealthResponseDto("ok", "Kinxter.Api")))
    .WithName("GetHealth")
    .WithTags("Health")
    .WithSummary("Returns API health status.")
    .Produces<HealthResponseDto>();

app.MapApiV1();

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await app.ApplyDatabaseMigrationsAsync();
}

app.Run();
