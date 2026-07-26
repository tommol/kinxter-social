using System.Globalization;
using Kinxter.Auth.Rendering;
using Kinxter.Auth.Rendering.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kinxter.Auth;

internal sealed class AuthPageRenderer
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    private readonly IRazorViewEngine viewEngine;
    private readonly IModelMetadataProvider metadataProvider;
    private readonly ITempDataProvider tempDataProvider;

    public AuthPageRenderer(
        IRazorViewEngine viewEngine,
        IModelMetadataProvider metadataProvider,
        ITempDataProvider tempDataProvider)
    {
        ArgumentNullException.ThrowIfNull(viewEngine);
        ArgumentNullException.ThrowIfNull(metadataProvider);
        ArgumentNullException.ThrowIfNull(tempDataProvider);

        this.viewEngine = viewEngine;
        this.metadataProvider = metadataProvider;
        this.tempDataProvider = tempDataProvider;
    }

    public Task<IResult> HomeAsync(HttpContext context, AuthRealmRegistry realmRegistry)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(realmRegistry);

        var realmOptions = context.GetAuthRealmOptions();

        return realmOptions is null
            ? RenderResultAsync(context, "/Views/Auth/Home.cshtml", new AuthServerHomeViewModel(realmRegistry.Realms))
            : RenderResultAsync(context, "/Views/Auth/RealmHome.cshtml", new AuthRealmHomeViewModel(realmOptions));
    }

    public Task<IResult> LoginAsync(
        HttpContext context,
        AuthOptions options,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/Login.cshtml",
            new AuthLoginPageViewModel(options, returnUrl, error),
            returnUrl);
    }

    public Task<IResult> RegisterAsync(
        HttpContext context,
        AuthOptions options,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/Register.cshtml",
            new AuthRegisterPageViewModel(options, returnUrl, error),
            returnUrl);
    }

    public Task<IResult> CheckEmailAsync(
        HttpContext context,
        AuthOptions options,
        string email,
        string? returnUrl)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/CheckEmail.cshtml",
            new AuthCheckEmailPageViewModel(options, email, returnUrl),
            returnUrl);
    }

    public Task<IResult> EmailConfirmedAsync(
        HttpContext context,
        AuthOptions options,
        string? returnUrl,
        bool succeeded)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/EmailConfirmed.cshtml",
            new AuthEmailConfirmedPageViewModel(options, returnUrl, succeeded),
            returnUrl);
    }

    public Task<IResult> ActivateInvitationAsync(
        HttpContext context,
        AuthOptions options,
        string email,
        string token,
        string? error = null,
        bool completed = false)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/ActivateInvitation.cshtml",
            new AuthActivateInvitationPageViewModel(options, email, token, error, completed));
    }

    public Task<IResult> LoginTwoFactorAsync(
        HttpContext context,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/LoginTwoFactor.cshtml",
            new AuthLoginTwoFactorPageViewModel(returnUrl, error),
            returnUrl);
    }

    public Task<IResult> TotpSetupAsync(
        HttpContext context,
        string? key,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/TotpSetup.cshtml",
            new AuthTotpSetupPageViewModel(key, returnUrl, error),
            returnUrl);
    }

    public Task<IResult> AccessDeniedAsync(HttpContext context)
    {
        return RenderResultAsync<object?>(context, "/Views/Auth/AccessDenied.cshtml", null);
    }

    public Task<IResult> DeviceVerificationAsync(
        HttpContext context,
        AuthDeviceVerificationPageViewModel model)
    {
        return RenderResultAsync(
            context,
            "/Views/Auth/DeviceVerification.cshtml",
            model);
    }

    private async Task<IResult> RenderResultAsync<TModel>(
        HttpContext context,
        string viewPath,
        TModel model,
        string? localeReturnUrl = null)
    {
        var html = await RenderAsync(context, viewPath, model, localeReturnUrl);

        return Results.Content(html, HtmlContentType);
    }

    private async Task<string> RenderAsync<TModel>(
        HttpContext context,
        string viewPath,
        TModel model,
        string? localeReturnUrl)
    {
        ArgumentNullException.ThrowIfNull(context);

        var actionContext = new ActionContext(
            context,
            context.GetRouteData(),
            new ActionDescriptor());
        var viewResult = this.viewEngine.GetView(
            executingFilePath: null,
            viewPath,
            isMainPage: true);

        if (!viewResult.Success)
        {
            var searchedLocations = string.Join(", ", viewResult.SearchedLocations);

            throw new InvalidOperationException($"Razor view '{viewPath}' was not found. Searched locations: {searchedLocations}.");
        }

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var viewData = new ViewDataDictionary<TModel>(
            this.metadataProvider,
            new ModelStateDictionary())
        {
            Model = model
        };
        viewData["ApplicationUrl"] = context
            .GetAuthRealmOptions()?
            .AllowedOrigins
            .FirstOrDefault();
        viewData["Locale"] = AuthUiText.ResolveLocale(context, localeReturnUrl);
        var tempData = new TempDataDictionary(context, this.tempDataProvider);
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);

        return writer.ToString();
    }
}
