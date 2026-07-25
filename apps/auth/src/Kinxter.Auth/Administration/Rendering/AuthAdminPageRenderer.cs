using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kinxter.Auth.Administration;

internal sealed class AuthAdminPageRenderer
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    private readonly IRazorViewEngine viewEngine;
    private readonly IModelMetadataProvider metadataProvider;
    private readonly ITempDataProvider tempDataProvider;
    private readonly IAntiforgery antiforgery;

    public AuthAdminPageRenderer(
        IRazorViewEngine viewEngine,
        IModelMetadataProvider metadataProvider,
        ITempDataProvider tempDataProvider,
        IAntiforgery antiforgery)
    {
        this.viewEngine = viewEngine;
        this.metadataProvider = metadataProvider;
        this.tempDataProvider = tempDataProvider;
        this.antiforgery = antiforgery;
    }

    public Task<IResult> LoginAsync(
        HttpContext context,
        AuthAdminOptions options,
        string returnUrl,
        string? error = null)
    {
        var model = new AuthAdminLoginPageViewModel(
            options.LoginPath,
            returnUrl,
            GetAntiforgeryToken(context),
            error);

        return RenderResultAsync(
            context,
            "/Views/AuthAdmin/Login.cshtml",
            model,
            error is null ? StatusCodes.Status200OK : StatusCodes.Status401Unauthorized);
    }

    public Task<IResult> DashboardAsync(
        HttpContext context,
        AuthAdminOptions options,
        string username,
        IReadOnlyList<AuthAdminRealmSummary> realms)
    {
        var model = new AuthAdminDashboardPageViewModel(
            username,
            options.PathBase,
            $"{options.PathBase}/logout",
            GetAntiforgeryToken(context),
            realms);

        return RenderResultAsync(
            context,
            "/Views/AuthAdmin/Dashboard.cshtml",
            model);
    }

    public Task<IResult> RealmAsync(
        HttpContext context,
        AuthAdminOptions options,
        string username,
        AuthAdminRealmDetails realm,
        AuthAdminUpdateRealmCommand? attemptedUpdate = null,
        string? error = null,
        bool saved = false)
    {
        var model = new AuthAdminRealmPageViewModel(
            username,
            options.PathBase,
            $"{options.PathBase}/logout",
            GetAntiforgeryToken(context),
            realm,
            attemptedUpdate,
            error,
            saved);

        return RenderResultAsync(
            context,
            "/Views/AuthAdmin/Realm.cshtml",
            model,
            error is null ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
    }

    private string GetAntiforgeryToken(HttpContext context)
    {
        return this.antiforgery.GetAndStoreTokens(context).RequestToken
            ?? throw new InvalidOperationException("An antiforgery request token could not be generated.");
    }

    private async Task<IResult> RenderResultAsync<TModel>(
        HttpContext context,
        string viewPath,
        TModel model,
        int statusCode = StatusCodes.Status200OK)
    {
        var html = await RenderAsync(context, viewPath, model);

        return Results.Content(html, HtmlContentType, statusCode: statusCode);
    }

    private async Task<string> RenderAsync<TModel>(
        HttpContext context,
        string viewPath,
        TModel model)
    {
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

            throw new InvalidOperationException(
                $"Razor view '{viewPath}' was not found. Searched locations: {searchedLocations}.");
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
