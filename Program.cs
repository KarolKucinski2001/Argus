using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
