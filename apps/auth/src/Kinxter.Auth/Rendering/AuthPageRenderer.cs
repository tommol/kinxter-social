using System.Globalization;
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

    public Task<IResult> HomeAsync(HttpContext context, AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var realmOptions = context.GetAuthRealmOptions();

        return realmOptions is null
            ? RenderResultAsync(context, "/Views/Auth/Home.cshtml", new AuthServerHomeViewModel(options))
            : RenderResultAsync(context, "/Views/Auth/RealmHome.cshtml", new AuthRealmHomeViewModel(realmOptions));
    }

    public Task<IResult> LoginAsync(
        HttpContext context,
        AuthOptions options,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(context, "/Views/Auth/Login.cshtml", new AuthLoginPageViewModel(options, returnUrl, error));
    }

    public Task<IResult> RegisterAsync(
        HttpContext context,
        AuthOptions options,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(context, "/Views/Auth/Register.cshtml", new AuthRegisterPageViewModel(options, returnUrl, error));
    }

    public Task<IResult> LoginTwoFactorAsync(
        HttpContext context,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(context, "/Views/Auth/LoginTwoFactor.cshtml", new AuthLoginTwoFactorPageViewModel(returnUrl, error));
    }

    public Task<IResult> TotpSetupAsync(
        HttpContext context,
        string? key,
        string? returnUrl,
        string? error = null)
    {
        return RenderResultAsync(context, "/Views/Auth/TotpSetup.cshtml", new AuthTotpSetupPageViewModel(key, returnUrl, error));
    }

    public Task<IResult> AccessDeniedAsync(HttpContext context)
    {
        return RenderResultAsync<object?>(context, "/Views/Auth/AccessDenied.cshtml", null);
    }

    private async Task<IResult> RenderResultAsync<TModel>(
        HttpContext context,
        string viewPath,
        TModel model)
    {
        var html = await RenderAsync(context, viewPath, model);

        return Results.Content(html, HtmlContentType);
    }

    private async Task<string> RenderAsync<TModel>(
        HttpContext context,
        string viewPath,
        TModel model)
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
