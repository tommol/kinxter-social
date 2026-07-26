using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Auth.Rendering.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    private static async Task<IResult> VerifyDeviceAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        AuthOptions authOptions,
        AuthPageRenderer renderer,
        IAntiforgery antiforgery)
    {
        var userCheck = await ValidateRealmUserAsync(
            context,
            userManager,
            signInManager,
            authOptions);

        if (userCheck.Result is not null)
        {
            return userCheck.Result;
        }

        return await RenderDeviceVerificationAsync(
            context,
            applicationManager,
            authOptions,
            renderer,
            antiforgery);
    }

    private static async Task<IResult> VerifyDeviceDecisionAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        IOpenIddictScopeManager scopeManager,
        IOpenIddictApplicationManager applicationManager,
        AuthOptions authOptions,
        AuthPageRenderer renderer,
        IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest();
        }

        var userCheck = await ValidateRealmUserAsync(
            context,
            userManager,
            signInManager,
            authOptions);

        if (userCheck.Result is not null)
        {
            return userCheck.Result;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var decision = form["decision"].ToString();
        var result = await context.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (result is not { Succeeded: true } ||
            string.IsNullOrWhiteSpace(result.Principal.GetClaim(Claims.ClientId)))
        {
            return await RenderDeviceVerificationAsync(
                context,
                applicationManager,
                authOptions,
                renderer,
                antiforgery,
                "The device code is invalid or has expired.");
        }

        if (string.Equals(decision, "deny", StringComparison.Ordinal))
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties
                {
                    RedirectUri = context.Request.PathBase.HasValue
                        ? context.Request.PathBase.Value
                        : "/"
                });
        }

        if (!string.Equals(decision, "accept", StringComparison.Ordinal))
        {
            return Results.BadRequest();
        }

        var principal = await CreatePrincipalAsync(
            userCheck.User!,
            userManager,
            scopeManager,
            result.Principal.GetScopes(),
            authOptions);

        return Results.SignIn(
            principal,
            new AuthenticationProperties
            {
                RedirectUri = context.Request.PathBase.HasValue
                    ? context.Request.PathBase.Value
                    : "/"
            },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> RenderDeviceVerificationAsync(
        HttpContext context,
        IOpenIddictApplicationManager applicationManager,
        AuthOptions authOptions,
        AuthPageRenderer renderer,
        IAntiforgery antiforgery,
        string? error = null)
    {
        var result = await context.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var userCode = result.Properties?.GetTokenValue(
            OpenIddictServerAspNetCoreConstants.Tokens.UserCode);
        string? applicationName = null;
        string[] scopes = [];

        if (result is { Succeeded: true } &&
            !string.IsNullOrWhiteSpace(result.Principal.GetClaim(Claims.ClientId)))
        {
            var clientId = result.Principal.GetClaim(Claims.ClientId)!;
            var application = await applicationManager.FindByClientIdAsync(
                clientId,
                context.RequestAborted);

            if (application is not null)
            {
                applicationName = await applicationManager.GetDisplayNameAsync(
                    application,
                    context.RequestAborted) ?? clientId;
                scopes = result.Principal.GetScopes().ToArray();
            }
        }
        else if (!string.IsNullOrWhiteSpace(context.Request.Query[Parameters.UserCode]))
        {
            userCode = context.Request.Query[Parameters.UserCode].ToString();
            error ??= "The device code is invalid or has expired.";
        }

        var verificationPath = $"{context.Request.PathBase}/connect/verify";
        var token = antiforgery.GetAndStoreTokens(context).RequestToken
            ?? throw new InvalidOperationException("An antiforgery token could not be generated.");

        return await renderer.DeviceVerificationAsync(
            context,
            new AuthDeviceVerificationPageViewModel(
                verificationPath,
                token,
                userCode,
                applicationName,
                scopes,
                error));
    }

    private static async Task<(AuthUser? User, IResult? Result)> ValidateRealmUserAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        AuthOptions authOptions)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null ||
            user.Realm != authOptions.Realm ||
            user.DeletedAt is not null ||
            user.DisabledAt is not null ||
            !await signInManager.CanSignInAsync(user))
        {
            await signInManager.SignOutAsync();

            return (null, Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedUrl()
            }));
        }

        if (authOptions.RequiresMfa && !user.TwoFactorEnabled)
        {
            var returnUrl = Uri.EscapeDataString(context.Request.GetEncodedUrl());
            return (null, Results.Redirect(
                $"{context.Request.PathBase}/account/manage/totp?returnUrl={returnUrl}"));
        }

        return (user, null);
    }
}
