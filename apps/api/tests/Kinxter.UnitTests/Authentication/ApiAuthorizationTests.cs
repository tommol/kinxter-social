using System.Security.Claims;
using Kinxter.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kinxter.UnitTests.Authentication;

public sealed class ApiAuthorizationTests
{
    [Fact]
    public async Task Backoffice_scope_and_realm_do_not_grant_access_without_user_permission()
    {
        await using var provider = CreateServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(
            new Claim("scope", "openid kinxter.admin"),
            new Claim("realm", "backoffice"));

        var genericAdmin = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            ApiAuthorizationPolicies.BackofficeAdmin);
        var monitoring = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            ApiAuthorizationPolicies.MonitoringRead);

        Assert.False(genericAdmin.Succeeded);
        Assert.False(monitoring.Succeeded);
    }

    [Fact]
    public async Task Backoffice_policies_require_their_specific_permission()
    {
        await using var provider = CreateServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var operations = CreatePrincipal(
            new Claim("scope", "openid kinxter.admin"),
            new Claim("realm", "backoffice"),
            new Claim("permission", ApiPermissions.AdminAccess),
            new Claim("permission", ApiPermissions.MonitoringRead));
        var wrongRealm = CreatePrincipal(
            new Claim("scope", "kinxter.admin"),
            new Claim("realm", "public"),
            new Claim("permission", ApiPermissions.MonitoringRead));

        Assert.True((await authorization.AuthorizeAsync(
            operations,
            null,
            ApiAuthorizationPolicies.BackofficeAdmin)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            operations,
            null,
            ApiAuthorizationPolicies.MonitoringRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            operations,
            null,
            ApiAuthorizationPolicies.UsersManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            wrongRealm,
            null,
            ApiAuthorizationPolicies.MonitoringRead)).Succeeded);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:PublicIssuer"] = "https://auth.example/realms/public",
                ["Auth:BackofficeIssuer"] = "https://auth.example/realms/backoffice",
                ["Auth:PublicRealm"] = "public",
                ["Auth:BackofficeRealm"] = "backoffice"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKinxterApiAuthentication(configuration);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test", "name", "role"));
}
