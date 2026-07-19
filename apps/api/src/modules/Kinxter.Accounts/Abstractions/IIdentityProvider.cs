namespace Kinxter.Accounts.Abstractions;

public interface IIdentityProvider
{
    Task<CreateIdentityUserResult> CreateUserAsync(
        CreateIdentityUserRequest request,
        CancellationToken cancellationToken = default);

    Task DisableUserAsync(
        string subject,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(
        string subject,
        CancellationToken cancellationToken = default);
}
