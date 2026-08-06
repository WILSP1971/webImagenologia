using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

public class EsculapioApiClientTests : IClassFixture<EsculapioApiClientWebApplicationFactory>
{
    private readonly EsculapioApiClientWebApplicationFactory _factory;

    public EsculapioApiClientTests(EsculapioApiClientWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ObtenerServidoresAsync_DeserializesMockJson()
    {
        using var scope = _factory.Services.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IEsculapioApiClient>();

        var servidores = (await apiClient.ObtenerServidoresAsync()).ToList();

        Assert.Single(servidores);
        Assert.Equal("Valle Salud", servidores[0].Descripcion);
        Assert.Equal("192.168.12.251", servidores[0].IpConexion);
        Assert.Equal("bd", servidores[0].BdConexion);
        Assert.Equal("3306", servidores[0].PortConexion);
    }

    [Fact]
    public async Task ValidarConexionAsync_InvalidCredentials_ThrowsUnauthorized()
    {
        using var scope = _factory.Services.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IEsculapioApiClient>();

        await Assert.ThrowsAsync<EsculapioApiException>(() =>
            apiClient.ValidarConexionAsync("192.168.12.251", "bad", "bad"));
    }

    [Fact]
    public async Task ValidarConexionAsync_ValidCredentials_ReturnsUsuario()
    {
        using var scope = _factory.Services.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IEsculapioApiClient>();

        var usuario = await apiClient.ValidarConexionAsync("192.168.12.251", "jgarcia", "lili2004");

        Assert.Equal("jgarcia", usuario.Usuario);
        Assert.Equal("JORGE GARCIA CASALETT", usuario.NombreCompleto);
        Assert.Equal("Administrador", usuario.Rol);
    }
}

public class EsculapioApiClientWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var mockResponses = new Dictionary<string, (HttpStatusCode StatusCode, string Body)>
        {
            ["Usuarios/obtener-servidores"] = (
                HttpStatusCode.OK,
                """
                [
                  {
                    "Descripcion": "Valle Salud",
                    "Ip_Conexion": "192.168.12.251",
                    "BaseDatos": "bd",
                    "Puerto": 3306
                  }
                ]
                """),
            ["Usuario=bad&"] = (
                HttpStatusCode.OK,
                """
                "Error Usuario y/o Password:"
                """),
            ["Usuario=jgarcia&"] = (
                HttpStatusCode.OK,
                """
                [
                  {
                    "Username": "jgarcia",
                    "Nombre": "JORGE GARCIA CASALETT",
                    "role": "Administrador"
                  }
                ]
                """)
        };

        Program.ConfigureTestServices = services =>
        {
            services.AddHttpClient<IEsculapioApiClient, EsculapioApiClient>((_, client) =>
                {
                    client.BaseAddress = new Uri("https://test.local/api/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new MockApiMessageHandler(mockResponses));
        };
    }

    protected override void Dispose(bool disposing)
    {
        Program.ConfigureTestServices = null;
        base.Dispose(disposing);
    }
}
