using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

public class StartupWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly FakeEsculapioApiClient _apiClient = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        Program.ConfigureTestServices = services =>
        {
            services.AddSingleton<IEsculapioApiClient>(_apiClient);
        };
    }

    protected override void Dispose(bool disposing)
    {
        Program.ConfigureTestServices = null;
        base.Dispose(disposing);
    }
}
