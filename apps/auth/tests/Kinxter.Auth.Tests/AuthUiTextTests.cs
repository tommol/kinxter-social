using Kinxter.Auth.Rendering;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kinxter.Auth.Tests;

public sealed class AuthUiTextTests
{
    [Fact]
    public void Locale_is_read_from_oidc_return_url()
    {
        var context = new DefaultHttpContext();
        var returnUrl = "/realms/kinxter/connect/authorize?client_id=kinxter-web&ui_locales=pl";

        var locale = AuthUiText.ResolveLocale(context, returnUrl);

        Assert.Equal("pl", locale);
    }

    [Fact]
    public void Locale_falls_back_to_supported_accept_language()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptLanguage = "de-DE;q=0.9, pl-PL;q=0.8, en;q=0.7";

        var locale = AuthUiText.ResolveLocale(context);

        Assert.Equal("pl", locale);
    }

    [Fact]
    public void Polish_copy_is_selected_for_polish_locale()
    {
        var text = AuthUiText.For("pl-PL");

        Assert.Equal("Zaloguj się", text["Sign in"]);
        Assert.Equal("Kontynuuj przez Google", text.Format("Continue with {0}", "Google"));
    }

    [Fact]
    public void English_copy_is_the_default()
    {
        var text = AuthUiText.For(null);

        Assert.Equal("Sign in", text["Sign in"]);
    }
}
