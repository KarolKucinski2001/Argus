using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class AuthenticationContractTests
{
    [Fact]
    public async Task CurrentUser_rejects_unauthenticated_request_with_problem_details()
    {
        await using var factory = new RealSchemeFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal(401, problem?.Status);
        Assert.Equal("Authentication required", problem?.Title);
    }

    [Fact]
    public async Task CurrentUser_returns_authenticated_principal()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal("test-subject", currentUser?.SubjectId);
        Assert.Equal("Test User", currentUser?.DisplayName);
        Assert.Equal("test@example.test", currentUser?.Email);
    }

    [Fact]
    public void Production_startup_rejects_missing_required_entra_configuration()
    {
        using var factory = new MissingConfigurationFactory();

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Entra:Authority", exception.ToString());
    }

    private sealed record CurrentUserResponse(string SubjectId, string? DisplayName, string? Email);

    private sealed record ProblemDetailsResponse(int? Status, string? Title);
}

internal static class TestEntraConfiguration
{
    public static void Apply(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Authority"] = "https://test.invalid",
                ["Entra:TenantId"] = "test-tenant",
                ["Entra:ClientId"] = "test-client",
                ["Entra:ClientSecret"] = "test-secret",
                ["Entra:CallbackPath"] = "/signin-oidc"
            });
        });
}

/// <summary>
/// Runs the application with its real authentication schemes so the unauthenticated
/// contract is asserted against production behaviour rather than a test handler.
/// </summary>
internal sealed class RealSchemeFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
    }
}

internal sealed class AuthenticationTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test",
                    options => options.ClaimsIssuer = "TestIssuer");

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
                options.DefaultSignInScheme = "Test";
                options.DefaultSignOutScheme = "Test";
            });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });
        });
    }
}

internal sealed class MissingConfigurationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Authority"] = "",
                ["Entra:TenantId"] = "",
                ["Entra:ClientId"] = "",
                ["Entra:ClientSecret"] = "",
                ["Entra:CallbackPath"] = ""
            });
        });
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "test-subject"),
            new Claim("name", "Test User"),
            new Claim("email", "test@example.test")
        ],
        Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
