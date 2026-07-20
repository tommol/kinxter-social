namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/account");

        group.MapGet("/register", GetRegisterAsync);
        group.MapPost("/register", RegisterAsync);
        group.MapGet("/login", GetLoginAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/external-login/{provider}", ExternalLoginAsync);
        group.MapGet("/external-callback", ExternalLoginCallbackAsync);
        group.MapGet("/login-2fa", GetLoginTwoFactorAsync);
        group.MapPost("/login-2fa", LoginTwoFactorAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/access-denied", GetAccessDeniedAsync);
        group.MapGet("/confirm-email", ConfirmEmailAsync);
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);

        group.MapGet("/manage/totp", TotpSetupAsync).RequireAuthorization();
        group.MapPost("/manage/totp", EnableTotpAsync).RequireAuthorization();
        group.MapPost("/manage/recovery-codes", GenerateRecoveryCodesAsync).RequireAuthorization();
        group.MapPost("/manage/external-logins/{provider}/link", LinkExternalLoginAsync).RequireAuthorization();

        group.MapPost("/passkeys/options/create", CreatePasskeyOptionsAsync).RequireAuthorization();
        group.MapPost("/passkeys/register", RegisterPasskeyAsync).RequireAuthorization();
        group.MapPost("/passkeys/options/request", RequestPasskeyOptionsAsync);
        group.MapPost("/passkeys/login", LoginWithPasskeyAsync);

        return app;
    }
}
