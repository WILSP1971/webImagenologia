using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebImagenologia.Tests;

public class StartupTests : IClassFixture<StartupWebApplicationFactory>
{
    private readonly StartupWebApplicationFactory _factory;

    public StartupTests(StartupWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_StartsSuccessfully()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Root_RedirectsToLogin_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
