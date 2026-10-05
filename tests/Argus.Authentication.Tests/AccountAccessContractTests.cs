using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class AccountAccessContractTests
{
    [Fact]
    public async Task Public_entry_renders_purpose_and_educational_boundary()
    {
        await using var factory = new AccountAccessFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Argus", html);
        Assert.Contains("educational", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not investment advice", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/account/signin", html);
    }

    [Fact]
    public async Task Sign_in_explicitly_challenges_openid_connect_and_does_not_preserve_external_return_url()
    {
        await using var factory = new AccountAccessFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            "/account/signin?returnUrl=https%3A%2F%2Fevil.example%2Fsteal");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("evil.example", response.Headers.Location?.ToString());
        Assert.Contains("test.invalid", response.Headers.Location?.ToString());
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, TestAuthenticationService.LastChallengeScheme);
    }

    [Fact]
    public async Task Authenticated_account_renders_available_claims()
    {
        await using var factory = new AuthenticatedAccountFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/account");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("test-subject", html);
        Assert.Contains("Test User", html);
        Assert.Contains("test@example.test", html);
        Assert.Contains("/account/signout", html);
    }

    [Fact]
    public async Task Unauthenticated_account_uses_explicit_provider_challenge_instead_of_api_problem_details()
    {
        await using var factory = new AccountAccessFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/account");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("test.invalid", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Account_error_renders_bounded_message_without_query_details()
    {
        await using var factory = new AccountAccessFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            "/account/error?code=provider-failure&exception=client_secret=super-secret");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("could not complete", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret", html);
        Assert.DoesNotContain("client_secret", html);
    }

    [Fact]
    public async Task Logout_signs_out_cookie_and_provider_without_external_return_url()
    {
        await using var factory = new AccountAccessFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            "/account/signout?returnUrl=https%3A%2F%2Fevil.example%2Fafter-logout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        Assert.Equal(
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            ],
            TestAuthenticationService.LastSignOutSchemes);
    }

    private sealed class AccountAccessFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            TestEntraConfiguration.Apply(builder);
            builder.ConfigureServices(services =>
            {
                TestAuthenticationService.Reset();
                services.Replace(ServiceDescriptor.Singleton<IAuthenticationService, TestAuthenticationService>());
            });
        }
    }

    private sealed class AuthenticatedAccountFactory : WebApplicationFactory<Program>
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

}

internal sealed class TestAuthenticationService : IAuthenticationService
{
    public static string? LastChallengeScheme { get; private set; }
    public static List<string?> LastSignOutSchemes { get; } = [];

    public static void Reset()
    {
        LastChallengeScheme = null;
        LastSignOutSchemes.Clear();
    }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult.NoResult());

    public Task ChallengeAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties)
    {
        LastChallengeScheme = scheme;
        context.Response.Redirect("https://test.invalid/authorize");
        return Task.CompletedTask;
    }

    public Task ForbidAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    public Task SignInAsync(
        HttpContext context,
        string? scheme,
        ClaimsPrincipal principal,
        AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignOutAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties)
    {
        LastSignOutSchemes.Add(scheme);
        if (context.Response.StatusCode == StatusCodes.Status200OK)
        {
            context.Response.Redirect(properties?.RedirectUri ?? "/");
        }

        return Task.CompletedTask;
    }
}
