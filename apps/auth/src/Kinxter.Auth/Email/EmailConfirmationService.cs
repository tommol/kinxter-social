using System.Net;
using System.Text;
using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Kinxter.Auth.Email;

internal sealed class EmailConfirmationService
{
    private readonly UserManager<AuthUser> userManager;
    private readonly AuthEmailOutboxWriter outbox;

    public EmailConfirmationService(UserManager<AuthUser> userManager, AuthEmailOutboxWriter outbox)
    {
        this.userManager = userManager;
        this.outbox = outbox;
    }

    public async Task QueueAsync(
        AuthUser user,
        AuthOptions options,
        string returnUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("A confirmation email cannot be sent without an email address.");
        }

        var token = await this.userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationUrl = QueryHelpers.AddQueryString(
            $"{options.Issuer}/account/confirm-email",
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id.ToString("D"),
                ["code"] = encodedToken,
                ["returnUrl"] = returnUrl,
                ["ui_locales"] = locale
            });
        var isPolish = string.Equals(locale, "pl", StringComparison.OrdinalIgnoreCase);
        var subject = isPolish ? "Potwierdź adres e-mail w Kinxter" : "Confirm your Kinxter email";
        var heading = isPolish ? "Potwierdź swój adres e-mail" : "Confirm your email address";
        var action = isPolish ? "Potwierdź e-mail" : "Confirm email";
        var hint = isPolish
            ? "Link jest ważny przez 24 godziny. Jeśli to nie Ty zakładasz konto, zignoruj tę wiadomość."
            : "This link is valid for 24 hours. If you did not create this account, ignore this message.";
        var encodedUrl = WebUtility.HtmlEncode(confirmationUrl);
        var html = $"<h1>{heading}</h1><p><a href=\"{encodedUrl}\">{action}</a></p><p>{hint}</p>";
        var text = $"{heading}\n\n{confirmationUrl}\n\n{hint}";

        await this.outbox.AddAsync(user.Email, subject, html, text, cancellationToken);
    }

    public static bool TryDecodeToken(string code, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            return true;
        }
        catch (FormatException)
        {
            token = "";
            return false;
        }
    }
}
