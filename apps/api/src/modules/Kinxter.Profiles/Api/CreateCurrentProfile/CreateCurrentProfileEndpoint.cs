using System.Security.Claims;
using Kinxter.Profiles.Application.CreateCurrentProfile;
using Kinxter.Profiles.Contracts.Dtos;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Profiles.Api.CreateCurrentProfile;

internal static class CreateCurrentProfileEndpoint
{
    public static IEndpointRouteBuilder MapCreateCurrentProfileEndpoint(
        this IEndpointRouteBuilder app,
        string publicUserPolicy)
    {
        app.MapPost("/me", HandleAsync)
            .RequireAuthorization(publicUserPolicy)
            .WithName("CreateCurrentProfile")
            .WithSummary("Creates the current public user's profile after the account is initialized.")
            .Produces<ProfileResponseDto>(StatusCodes.Status201Created)
            .Produces<ProfileResponseDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        CreateCurrentProfileRequestDto request,
        ClaimsPrincipal principal,
        ICommandHandler<CreateCurrentProfileCommand, CreateCurrentProfileResult> handler,
        CancellationToken cancellationToken)
    {
        var validationErrors = ProfileEndpointValidation.ValidateRequiredText(
        [
            (nameof(request.Handle), request.Handle, Profile.HandleMaxLength),
            (nameof(request.DisplayName), request.DisplayName, Profile.DisplayNameMaxLength)
        ]);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var identity = ProfileEndpointIdentityReader.GetIdentity(principal);
        var result = await handler.HandleAsync(
            new CreateCurrentProfileCommand(
                identity.IdentityProvider,
                identity.Subject,
                request.Handle,
                request.DisplayName),
            cancellationToken);

        return result.Status switch
        {
            CreateCurrentProfileStatus.Created => Results.Created(
                $"/api/v1/profiles/{result.Profile!.Id}",
                ProfileResponseDto.From(result.Profile)),
            CreateCurrentProfileStatus.AlreadyCreated => Results.Ok(ProfileResponseDto.From(result.Profile!)),
            CreateCurrentProfileStatus.AccountNotInitialized => Results.Conflict(
                new { error = "Account is not initialized yet." }),
            CreateCurrentProfileStatus.HandleAlreadyTaken => Results.Conflict(
                new { error = "Handle is already taken." }),
            _ => throw new InvalidOperationException($"Unsupported create profile status '{result.Status}'.")
        };
    }
}
