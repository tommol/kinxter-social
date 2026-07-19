using Kinxter.Accounts.Abstractions;
using Kinxter.Accounts.Contracts.Events;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Accounts.Model;
using Kinxter.Shared.Abstractions.Application;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Accounts.Application.RegisterAccount;

public sealed class RegisterAccountHandler : ICommandHandler<RegisterAccountCommand, RegisterAccountResult>
{
    private readonly AccountsDbContext dbContext;
    private readonly IIdentityProvider identityProvider;
    private readonly IOutboxWriter<AccountsOutbox> outboxWriter;
    private readonly IClock clock;

    public RegisterAccountHandler(
        AccountsDbContext dbContext,
        IIdentityProvider identityProvider,
        IOutboxWriter<AccountsOutbox> outboxWriter,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(identityProvider);
        ArgumentNullException.ThrowIfNull(outboxWriter);
        ArgumentNullException.ThrowIfNull(clock);

        this.dbContext = dbContext;
        this.identityProvider = identityProvider;
        this.outboxWriter = outboxWriter;
        this.clock = clock;
    }

    public async Task<RegisterAccountResult> HandleAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new CreateIdentityUserRequest(
            command.Email,
            command.Password);

        var result = await this.identityProvider.CreateUserAsync(request, cancellationToken);
        var now = this.clock.UtcNow;

        var account = Account.Create(
            Guid.CreateVersion7(now),
            result.Email,
            result.Provider,
            result.Subject,
            result.EmailVerified,
            now);

        var accountCreated = new AccountCreated(
            Guid.CreateVersion7(now),
            now,
            account.Id,
            command.Handle,
            command.DisplayName);

        this.dbContext.Accounts.Add(account);

        await this.outboxWriter.AddAsync(accountCreated, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        var status = account.Status == AccountStatus.Active
            ? RegisterAccountStatus.Registered
            : RegisterAccountStatus.PendingEmailVerification;

        return new RegisterAccountResult(account.Id, status);
    }
}
