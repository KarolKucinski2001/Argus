using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.MarketData;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();

builder.Services.AddOptions<MarketDataOptions>()
    .Bind(builder.Configuration.GetSection(MarketDataOptions.SectionName))
    .Validate(
        options => options.Validate(Options.DefaultName, options).Succeeded,
        "MarketData configuration is invalid.")
    .ValidateOnStart();
builder.Services.AddSingleton<IMarketDataCache, InMemoryMarketDataCache>();
builder.Services.AddSingleton<IMarketDataProvider, UnavailableMarketDataProvider>();

var entraSection = builder.Configuration.GetSection("Entra");
builder.Services.AddOptions<EntraOptions>()
    .Bind(entraSection)
    .Validate(
        options => builder.Environment.IsDevelopment() || options.HasRequiredValues,
        "Entra:Authority, Entra:TenantId, Entra:ClientId, Entra:ClientSecret, and Entra:CallbackPath are required outside Development.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // API routes must answer 401 instead of redirecting to the identity provider.
        // Interactive sign-in challenges OpenIdConnect explicitly by scheme name.
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-Argus.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Authentication required",
                    Detail = "Authenticate before accessing this resource.",
                    Instance = context.Request.Path
                },
                options: null,
                contentType: "application/problem+json");
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = entraSection["Authority"];
        options.ClientId = entraSection["ClientId"];
        options.ClientSecret = entraSection["ClientSecret"];
        options.CallbackPath = entraSection["CallbackPath"] ?? "/signin-oidc";
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = false;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.RequireHttpsMetadata = true;
        options.Events.OnAccessDenied = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/account/error?code=access-denied");
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/account/error?code=provider-failure");
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    var subjectId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authenticated subject unavailable",
            detail: "The authenticated principal does not contain a subject identifier.");
    }

    return Results.Ok(new CurrentUserResponse(
        subjectId,
        user.FindFirstValue("name") ?? user.Identity?.Name,
        user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email")));
})
.WithName("GetCurrentUser")
.RequireAuthorization();

app.MapPost("/api/market-data", async (
    MarketDataRequest? request,
    IMarketDataProvider provider,
    IMarketDataCache cache,
    IOptions<MarketDataOptions> configuredOptions,
    CancellationToken cancellationToken) =>
{
    var options = configuredOptions.Value;
    var validationError = ValidateMarketDataRequest(request, options);
    if (validationError is not null)
    {
        return validationError;
    }

    if (!options.PublicDisplayApproved || string.IsNullOrWhiteSpace(options.ProviderId))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Market data service unavailable",
            detail: "Market data remains disabled until an approved provider and public-display terms are configured.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCategory"] = MarketDataErrorCategory.ProviderNotApproved.ToString(),
                ["retryable"] = false
            });
    }

    var validRequest = request!;
    var results = new List<MarketDataResult>();
    var uncachedAssets = new List<AssetReference>();
    var expiredEntries = new Dictionary<string, MarketDataResult>(StringComparer.OrdinalIgnoreCase);

    foreach (var asset in validRequest.Assets)
    {
        var key = new MarketDataCacheKey(
            asset.Id,
            asset.AssetClass,
            validRequest.History.Start,
            validRequest.History.End,
            validRequest.History.Granularity);
        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is null)
        {
            uncachedAssets.Add(asset);
        }
        else if (!cached.IsExpired)
        {
            results.Add(cached.Result);
        }
        else
        {
            expiredEntries[asset.Id] = AddWarning(
                cached.Result,
                new MarketDataWarning(
                    MarketDataWarningCategory.StaleData,
                    "Cached evidence has expired and is being refreshed.",
                    true));
            uncachedAssets.Add(asset);
        }
    }

    if (uncachedAssets.Count > 0)
    {
        IReadOnlyList<MarketDataResult> providerResults;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.RequestTimeout);
            providerResults = await provider.GetAsync(
                validRequest with { Assets = uncachedAssets },
                timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            providerResults = uncachedAssets.Select(asset => ErrorResult(
                asset,
                validRequest.History,
                new MarketDataError(MarketDataErrorCategory.Timeout, "The market data provider timed out.", true))).ToArray();
        }
        catch (HttpRequestException)
        {
            providerResults = uncachedAssets.Select(asset => ErrorResult(
                asset,
                validRequest.History,
                new MarketDataError(MarketDataErrorCategory.UpstreamUnavailable, "The market data provider is unavailable.", true))).ToArray();
        }

        foreach (var result in providerResults)
        {
            if (result.Error is null)
            {
                var key = new MarketDataCacheKey(
                    result.Asset.Id,
                    result.Asset.AssetClass,
                    validRequest.History.Start,
                    validRequest.History.End,
                    validRequest.History.Granularity);
                await cache.SetAsync(
                    new MarketDataCacheEntry(
                        key,
                        result,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow.Add(options.CacheDuration)),
                    cancellationToken);
                results.Add(result);
            }
            else if (expiredEntries.TryGetValue(result.Asset.Id, out var expired))
            {
                results.Add(AddWarning(
                    expired with { Error = result.Error },
                    new MarketDataWarning(
                        MarketDataWarningCategory.UpstreamUnavailable,
                        "Fresh evidence was unavailable; the expired cached evidence is shown.",
                        true)));
            }
            else
            {
                results.Add(result);
            }
        }
    }

    var orderedResults = validRequest.Assets.Select(asset =>
        results.FirstOrDefault(result => string.Equals(result.Asset.Id, asset.Id, StringComparison.OrdinalIgnoreCase))
        ?? ErrorResult(
            asset,
            validRequest.History,
            new MarketDataError(
                MarketDataErrorCategory.UpstreamUnavailable,
                "The market data provider returned no result for this asset.",
                true))).ToArray();

    return Results.Ok(new MarketDataResponse(orderedResults));
})
.WithName("GetMarketData")
.RequireAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

static IResult? ValidateMarketDataRequest(MarketDataRequest? request, MarketDataOptions options)
{
    if (request is null || !request.IsValid)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid market data request",
            detail: "Assets and a valid historical range are required.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCategory"] = MarketDataErrorCategory.InvalidRequest.ToString(),
                ["retryable"] = false
            });
    }

    if (request.Assets.Count > options.MaximumSelectedAssets)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Too many assets requested",
            detail: $"A maximum of {options.MaximumSelectedAssets} assets may be requested.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCategory"] = MarketDataErrorCategory.InvalidRequest.ToString(),
                ["retryable"] = false
            });
    }

    if (request.Assets.GroupBy(asset => asset.Id, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Duplicate asset requested",
            detail: "Each asset may be requested only once.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCategory"] = MarketDataErrorCategory.InvalidRequest.ToString(),
                ["retryable"] = false
            });
    }

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    if (request.History.End > today ||
        request.History.Start < request.History.End.AddMonths(-MarketDataOptions.RequiredHistoryMonths))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid market data history range",
            detail: $"The requested range must end today or earlier and span no more than {MarketDataOptions.RequiredHistoryMonths} months.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCategory"] = MarketDataErrorCategory.InvalidRequest.ToString(),
                ["retryable"] = false
            });
    }

    var unsupported = request.Assets
        .FirstOrDefault(asset => !options.AllowedAssets.TryGetValue(asset.Id, out var assetClass) ||
                                 assetClass != asset.AssetClass);
    if (unsupported is not null)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Unsupported asset",
            detail: $"Asset '{unsupported.Id}' is not in the configured market-data allowlist.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCategory"] = MarketDataErrorCategory.UnsupportedAsset.ToString(),
                ["retryable"] = false
            });
    }

    return null;
}

static MarketDataResult ErrorResult(
    AssetReference asset,
    MarketDataHistoryRequest history,
    MarketDataError error)
{
    var now = DateTimeOffset.UtcNow;
    return new MarketDataResult(
        asset,
        history,
        null,
        [],
        null,
        new MarketDataFreshness(now, now, null, true),
        MarketDataQuality.Unavailable,
        [],
        error);
}

static MarketDataResult AddWarning(MarketDataResult result, MarketDataWarning warning) =>
    result with
    {
        Freshness = result.Freshness with { IsStale = true },
        Quality = result.Quality == MarketDataQuality.Complete
            ? MarketDataQuality.Stale
            : result.Quality,
        Warnings = result.Warnings.Append(warning).ToArray()
    };

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record CurrentUserResponse(string SubjectId, string? DisplayName, string? Email);

sealed class EntraOptions
{
    public string? Authority { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CallbackPath { get; set; }

    public bool HasRequiredValues =>
        !string.IsNullOrWhiteSpace(Authority) &&
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(CallbackPath);
}

public partial class Program;
