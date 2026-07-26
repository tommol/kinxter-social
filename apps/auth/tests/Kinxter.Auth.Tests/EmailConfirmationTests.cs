using System.Text;
using Kinxter.Auth.Email;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Kinxter.Auth.Tests;

public sealed class EmailConfirmationTests
{
    [Fact]
    public void Confirmation_token_uses_url_safe_round_trip_encoding()
    {
        const string token = "identity/token+with=special/value";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var decoded = EmailConfirmationService.TryDecodeToken(encoded, out var result);

        Assert.True(decoded);
        Assert.Equal(token, result);
    }

    [Fact]
    public void Malformed_confirmation_token_is_rejected()
    {
        var decoded = EmailConfirmationService.TryDecodeToken("%%%", out var result);

        Assert.False(decoded);
        Assert.Empty(result);
    }
}
