using Kinxter.Profiles.Api.CompleteProfileOnboarding;
using Kinxter.Profiles.Api.CreateCurrentProfile;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Profiles.Api;

public static class ProfilesEndpoints
{
    public static IEndpointRouteBuilder MapProfilesEndpoints(
        this IEndpointRouteBuilder app,
        string publicUserPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicUserPolicy);

        var group = app.MapGroup("/profiles")
            .WithTags("Profiles");

        group.MapCreateCurrentProfileEndpoint(publicUserPolicy);
        group.MapUpdateCurrentProfileEndpoints(publicUserPolicy);
        group.MapGetProfileEndpoint(publicUserPolicy);

        return app;
    }
}
