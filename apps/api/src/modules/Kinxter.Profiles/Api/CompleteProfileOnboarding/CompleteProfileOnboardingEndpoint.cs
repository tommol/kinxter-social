using System.Security.Claims;
using Kinxter.Profiles.Application.CompleteProfileOnboarding;
using Kinxter.Profiles.Contracts.Dtos;
using Kinxter.Shared.Abstractions.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Profiles.Api.CompleteProfileOnboarding;

internal static class CompleteProfileOnboardingEndpoint
{
    public static IEndpointRouteBuilder MapCompleteProfileOnboardingEndpoint(
        this IEndpointRouteBuilder app,
        string publicUserPolicy)
    {
        app.MapPut("/me/onboarding", HandleAsync)
            .RequireAuthorization(publicUserPolicy)
            .WithName("CompleteProfileOnboarding")
            .WithSummary("Completes profile onboarding with additional profile information.")
            .Produces<ProfileResponseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        CompleteProfileOnboardingRequestDto request,
        ClaimsPrincipal principal,
        ICommandHandler<CompleteProfileOnboardingCommand, CompleteProfileOnboardingResult> handler,
        CancellationToken cancellationToken)
    {
        var validationErrors = ProfileEndpointValidation.ValidateProfileOnboarding(
            request.Bio,
            request.ProfilePictureUrl);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var identity = ProfileEndpointIdentityReader.GetIdentity(principal);
        var result = await handler.HandleAsync(
            new CompleteProfileOnboardingCommand(
                identity.IdentityProvider,
                identity.Subject,
                request.Bio,
                request.ProfilePictureUrl),
            cancellationToken);

        return result.Status switch
        {
            CompleteProfileOnboardingStatus.Completed => Results.Ok(ProfileResponseDto.From(result.Profile!)),
            CompleteProfileOnboardingStatus.AccountNotInitialized => Results.Conflict(
                new { error = "Account is not initialized yet." }),
            CompleteProfileOnboardingStatus.ProfileNotCreated => Results.Conflict(
                new { error = "Profile must be created before onboarding can be completed." }),
            _ => throw new InvalidOperationException($"Unsupported profile onboarding status '{result.Status}'.")
        };
    }
}
