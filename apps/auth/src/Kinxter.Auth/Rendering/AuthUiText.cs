using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace Kinxter.Auth.Rendering;

internal sealed class AuthUiText
{
    private const string DefaultLocale = "en";

    private static readonly IReadOnlyDictionary<string, string> Polish =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Back to Kinxter"] = "Wróć do Kinxter",
            ["Secure account access"] = "Bezpieczny dostęp do konta",
            ["About Kinxter"] = "O Kinxter",
            ["Private by design"] = "Prywatność od podstaw",
            ["Meet openly. Stay in control."] = "Poznawaj otwarcie. Zachowaj kontrolę.",
            ["A discreet space to find your people, explore at your own pace and build connections without judgement."] = "Dyskretna przestrzeń, w której znajdziesz swoich ludzi, będziesz odkrywać we własnym tempie i budować relacje bez oceniania.",
            ["Kinxter principles"] = "Zasady Kinxter",
            ["Privacy from the start"] = "Prywatność od początku",
            ["Consent and boundaries"] = "Zgoda i granice",
            ["Active moderation"] = "Aktywna moderacja",
            ["Adults only"] = "Tylko dla dorosłych",
            ["Respect · Consent · Privacy"] = "Szacunek · Zgoda · Prywatność",
            ["Welcome back"] = "Witaj ponownie",
            ["Sign in"] = "Zaloguj się",
            ["Continue to your private Kinxter space."] = "Przejdź do swojej prywatnej przestrzeni Kinxter.",
            ["Invalid credentials."] = "Nieprawidłowy adres e-mail lub hasło.",
            ["Invalid authenticator code."] = "Nieprawidłowy kod uwierzytelniający.",
            ["External login could not be completed."] = "Nie udało się ukończyć logowania zewnętrznego.",
            ["External login provider is not available."] = "Zewnętrzny dostawca logowania jest niedostępny.",
            ["This account cannot sign in."] = "To konto nie może się zalogować.",
            ["External provider did not return a verified email address."] = "Zewnętrzny dostawca nie zwrócił zweryfikowanego adresu e-mail.",
            ["An account with this email already exists. Sign in with email and link this provider from your account."] = "Konto z tym adresem e-mail już istnieje. Zaloguj się adresem e-mail i połącz dostawcę z poziomu konta.",
            ["External login failed."] = "Logowanie zewnętrzne nie powiodło się.",
            ["Continue with {0}"] = "Kontynuuj przez {0}",
            ["or use email"] = "lub użyj e-maila",
            ["Email"] = "E-mail",
            ["Password"] = "Hasło",
            ["New to Kinxter?"] = "Nie masz jeszcze konta w Kinxter?",
            ["Create an account"] = "Utwórz konto",
            ["Registration"] = "Rejestracja",
            ["Signup disabled"] = "Rejestracja wyłączona",
            ["This space does not currently allow self-service registration."] = "Ta przestrzeń nie pozwala obecnie na samodzielną rejestrację.",
            ["Join at your pace"] = "Dołącz we własnym tempie",
            ["Create account"] = "Utwórz konto",
            ["Start with only the essentials. You decide what to share later."] = "Zacznij od podstawowych informacji. Później zdecydujesz, czym chcesz się podzielić.",
            ["Email and password are required."] = "Adres e-mail i hasło są wymagane.",
            ["Already have an account?"] = "Masz już konto?",
            ["Check your email"] = "Sprawdź swoją skrzynkę",
            ["Email verification"] = "Weryfikacja e-maila",
            ["We sent a confirmation link to your email address. Confirm it before signing in."] = "Wysłaliśmy link potwierdzający na Twój adres e-mail. Potwierdź go przed zalogowaniem.",
            ["Send the link again"] = "Wyślij link ponownie",
            ["Back to sign in"] = "Wróć do logowania",
            ["Email confirmed"] = "E-mail potwierdzony",
            ["Invalid confirmation link"] = "Nieprawidłowy link potwierdzający",
            ["Your account is ready. Sign in to continue."] = "Twoje konto jest gotowe. Zaloguj się, aby kontynuować.",
            ["The link is invalid or has expired. Request a new one from the sign-in page."] = "Link jest nieprawidłowy lub wygasł. Poproś o nowy na stronie logowania.",
            ["Confirm your email before signing in."] = "Potwierdź swój adres e-mail przed zalogowaniem.",
            ["Two-factor authentication"] = "Uwierzytelnianie dwuskładnikowe",
            ["One more step"] = "Jeszcze jeden krok",
            ["Verify it’s you"] = "Potwierdź, że to Ty",
            ["Enter the current code from your authenticator app."] = "Wpisz aktualny kod z aplikacji uwierzytelniającej.",
            ["Authenticator code"] = "Kod uwierzytelniający",
            ["Verify"] = "Potwierdź",
            ["Authorize device"] = "Autoryzuj urządzenie",
            ["Device connection"] = "Łączenie urządzenia",
            ["Authorize a device"] = "Autoryzuj urządzenie",
            ["Enter the code shown by the application or device."] = "Wpisz kod wyświetlony przez aplikację lub urządzenie.",
            ["Device code"] = "Kod urządzenia",
            ["Continue"] = "Kontynuuj",
            ["{0} is requesting access to this account."] = "Aplikacja {0} prosi o dostęp do tego konta.",
            ["Code"] = "Kod",
            ["Scopes"] = "Zakresy dostępu",
            ["No additional scopes"] = "Brak dodatkowych zakresów",
            ["Confirm only if this code exactly matches the code displayed on your device."] = "Potwierdź tylko wtedy, gdy ten kod dokładnie odpowiada kodowi wyświetlonemu na urządzeniu.",
            ["Authorize"] = "Autoryzuj",
            ["Deny"] = "Odrzuć",
            ["Account activated"] = "Konto aktywowane",
            ["Activate backoffice account"] = "Aktywuj konto administracyjne",
            ["Account ready"] = "Konto gotowe",
            ["Your password has been set. Sign in to configure the required multi-factor authentication."] = "Hasło zostało ustawione. Zaloguj się, aby skonfigurować wymagane uwierzytelnianie wieloskładnikowe.",
            ["Continue to sign in"] = "Przejdź do logowania",
            ["Secure invitation"] = "Bezpieczne zaproszenie",
            ["Set a password for {0}. MFA setup will be required during your first sign-in."] = "Ustaw hasło dla {0}. Podczas pierwszego logowania wymagane będzie skonfigurowanie MFA.",
            ["Confirm password"] = "Potwierdź hasło",
            ["Activate account"] = "Aktywuj konto",
            ["Passwords do not match."] = "Hasła nie są takie same.",
            ["The invitation is invalid or has already been used."] = "Zaproszenie jest nieprawidłowe lub zostało już wykorzystane.",
            ["Identity service"] = "Usługa tożsamości",
            ["Secure account access for this Kinxter space."] = "Bezpieczny dostęp do konta w tej przestrzeni Kinxter.",
            ["Realm"] = "Przestrzeń",
            ["Issuer"] = "Wystawca",
            ["MFA policy"] = "Polityka MFA",
            ["Choose the account space you want to access."] = "Wybierz przestrzeń konta, do której chcesz uzyskać dostęp.",
            ["Authenticator app"] = "Aplikacja uwierzytelniająca",
            ["Protect your account"] = "Chroń swoje konto",
            ["Set up MFA"] = "Skonfiguruj MFA",
            ["Enter this key in your authenticator app, then verify a current code."] = "Wpisz ten klucz w aplikacji uwierzytelniającej, a następnie potwierdź aktualny kod.",
            ["Verification code"] = "Kod weryfikacyjny",
            ["Enable MFA"] = "Włącz MFA",
            ["Access denied"] = "Brak dostępu",
            ["Account security"] = "Bezpieczeństwo konta",
            ["Your account cannot access this resource."] = "Twoje konto nie ma dostępu do tego zasobu."
        };

    private AuthUiText(string locale)
    {
        Locale = locale;
    }

    public string Locale { get; }

    public string this[string value] =>
        Locale == "pl" && Polish.TryGetValue(value, out var translated)
            ? translated
            : value;

    public string Format(string value, params object?[] arguments)
    {
        return string.Format(CultureInfo.GetCultureInfo(Locale), this[value], arguments);
    }

    public static AuthUiText For(string? locale)
    {
        return new AuthUiText(NormalizeLocale(locale) ?? DefaultLocale);
    }

    public static string ResolveLocale(HttpContext context, string? returnUrl = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        return NormalizeLocale(context.Request.Query["ui_locales"].ToString())
            ?? GetLocaleFromReturnUrl(returnUrl ?? context.Request.Query["returnUrl"].ToString())
            ?? NormalizeLocale(string.Join(
                ' ',
                context.Request.GetTypedHeaders().AcceptLanguage?
                    .OrderByDescending(language => language.Quality ?? 1)
                    .Select(language => language.Value.Value)
                    ?? []))
            ?? DefaultLocale;
    }

    private static string? GetLocaleFromReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var queryStart = returnUrl.IndexOf('?', StringComparison.Ordinal);

        if (queryStart < 0)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(returnUrl[queryStart..]);

        return NormalizeLocale(query["ui_locales"].ToString());
    }

    private static string? NormalizeLocale(string? localeCandidates)
    {
        if (string.IsNullOrWhiteSpace(localeCandidates))
        {
            return null;
        }

        foreach (var candidate in localeCandidates.Split(
                     [' ', ',', ';'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var language = candidate.Split('-', 2, StringSplitOptions.TrimEntries)[0].ToLowerInvariant();

            if (language is "pl" or "en")
            {
                return language;
            }
        }

        return null;
    }
}
