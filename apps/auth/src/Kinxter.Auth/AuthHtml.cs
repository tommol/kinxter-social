using System.Net;

namespace Kinxter.Auth;

internal static class AuthHtml
{
    public static string Home(AuthOptions options)
    {
        return Page(
            "Kinxter Auth",
            $"""
            <h1>Kinxter Auth</h1>
            <dl>
              <dt>Realm</dt><dd>{Encode(options.Realm)}</dd>
              <dt>Issuer</dt><dd>{Encode(options.Issuer)}</dd>
              <dt>MFA policy</dt><dd>{Encode(options.MfaPolicy.ToString())}</dd>
            </dl>
            <p><a href="/account/login">Sign in</a>{(options.SignupEnabled ? " · <a href=\"/account/register\">Create account</a>" : "")}</p>
            """);
    }

    public static string Register(AuthOptions options, string? returnUrl, string? error = null)
    {
        if (!options.SignupEnabled)
        {
            return Page("Signup disabled", "<h1>Signup disabled</h1><p>This realm does not allow self-service registration.</p>");
        }

        return Page(
            "Create account",
            $"""
            <h1>Create account</h1>
            {Error(error)}
            <form method="post">
              <input type="hidden" name="returnUrl" value="{Encode(returnUrl ?? "/")}">
              <label>Email <input type="email" name="email" autocomplete="email" required></label>
              <label>Password <input type="password" name="password" autocomplete="new-password" required></label>
              <button type="submit">Create account</button>
            </form>
            <p><a href="/account/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}">Sign in</a></p>
            """);
    }

    public static string Login(string? returnUrl, string? error = null)
    {
        return Page(
            "Sign in",
            $"""
            <h1>Sign in</h1>
            {Error(error)}
            <form method="post">
              <input type="hidden" name="returnUrl" value="{Encode(returnUrl ?? "/")}">
              <label>Email <input type="email" name="email" autocomplete="email" required></label>
              <label>Password <input type="password" name="password" autocomplete="current-password" required></label>
              <button type="submit">Sign in</button>
            </form>
            <p><a href="/account/register?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}">Create account</a></p>
            """);
    }

    public static string LoginTwoFactor(string? returnUrl, string? error = null)
    {
        return Page(
            "Two-factor authentication",
            $"""
            <h1>Two-factor authentication</h1>
            {Error(error)}
            <form method="post">
              <input type="hidden" name="returnUrl" value="{Encode(returnUrl ?? "/")}">
              <label>Authenticator code <input type="text" name="code" inputmode="numeric" autocomplete="one-time-code" required></label>
              <button type="submit">Verify</button>
            </form>
            """);
    }

    public static string TotpSetup(string? key, string? returnUrl, string? error = null)
    {
        return Page(
            "Authenticator app",
            $"""
            <h1>Authenticator app</h1>
            {Error(error)}
            <p>Enter this key in your authenticator app, then verify a current code.</p>
            <code>{Encode(key ?? "")}</code>
            <form method="post">
              <input type="hidden" name="returnUrl" value="{Encode(returnUrl ?? "/")}">
              <label>Code <input type="text" name="code" inputmode="numeric" autocomplete="one-time-code" required></label>
              <button type="submit">Enable MFA</button>
            </form>
            """);
    }

    public static string AccessDenied()
    {
        return Page("Access denied", "<h1>Access denied</h1><p>Your account cannot access this resource.</p>");
    }

    public static string Page(string title, string body)
    {
        return $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{Encode(title)}}</title>
          <style>
            body{font-family:system-ui,-apple-system,Segoe UI,sans-serif;margin:0;background:#f7f8fa;color:#182026}
            main{max-width:440px;margin:8vh auto;padding:32px;background:#fff;border:1px solid #d8dee5;border-radius:8px}
            h1{font-size:28px;margin:0 0 20px}
            label{display:block;margin:14px 0 6px;font-size:14px;font-weight:600}
            input{width:100%;box-sizing:border-box;padding:10px;border:1px solid #c8d0d8;border-radius:6px;font:inherit}
            button{margin-top:18px;padding:10px 14px;border:0;border-radius:6px;background:#1f6f5b;color:#fff;font-weight:700}
            code{display:block;overflow:auto;padding:10px;border-radius:6px;background:#eef3f7}
            .error{color:#b42318;background:#fff0ee;padding:10px;border-radius:6px}
            dl{display:grid;grid-template-columns:96px 1fr;gap:8px}
            dt{font-weight:700}
            dd{margin:0;overflow-wrap:anywhere}
          </style>
        </head>
        <body><main>{{body}}</main></body>
        </html>
        """;
    }

    private static string Error(string? error)
    {
        return string.IsNullOrWhiteSpace(error)
            ? ""
            : $"""<p class="error">{Encode(error)}</p>""";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
