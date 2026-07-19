using Kinxter.Accounts.Application.RegisterAccount;
using Kinxter.Accounts.Contracts.Dtos;
using Kinxter.Shared.Abstractions.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kinxter.Accounts.Api.RegisterAccount;

internal static class RegisterAccountEndpoint
{
    public static IEndpointRouteBuilder MapRegisterAccountEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", HandleAsync)
            .WithName("RegisterAccount")
            .Produces<RegisterAccountResponseDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        RegisterAccountRequestDto request,
        ICommandHandler<RegisterAccountCommand, RegisterAccountResult> handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterAccountCommand(
            request.Email,
            request.Password,
            request.Handle,
            request.DisplayName);

        var result = await handler.HandleAsync(command, cancellationToken);
        var response = RegisterAccountResponseDto.From(result);

        return Results.Created($"/api/v1/accounts/{response.AccountId}", response);
    }
}
