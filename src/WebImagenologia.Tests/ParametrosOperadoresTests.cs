using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Tests;

public class ParametrosOperadoresTests : IClassFixture<ParametrosOperadoresWebApplicationFactory>
{
    private readonly ParametrosOperadoresWebApplicationFactory _factory;

    public ParametrosOperadoresTests(ParametrosOperadoresWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Operadores_Get_RequiresAuthentication()
    {
        _factory.Reset();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Parametros/Operadores");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operadores_Get_AsAdmin_ReturnsOkWithGridContent()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Parametros/Operadores");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Operadores por Empresa", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1122334455", body, StringComparison.Ordinal);
        Assert.Contains("Empresa Demo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operadores_Registrar_AsAdmin_RedirectsWithSuccess()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Parametros/Operadores");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["CedulaOperador"] = "1122334455",
            ["UsuarioEsculapio"] = "mgonzalez",
            ["EmpresasSeleccionadas"] = "01",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Parametros/Operadores/Registrar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("registrado correctamente", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mgonzalez", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operadores_Eliminar_AsAdmin_RedirectsWithSuccess()
    {
        _factory.Reset();
        _factory.ApiClient.OperadoresRegistrados.Add(new OperadorRegistradoDto(
            "1122334455",
            "María González",
            "mgonzalez",
            ["01"]));

        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Parametros/Operadores");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["cedula"] = "1122334455",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Parametros/Operadores/Eliminar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("eliminado correctamente", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mgonzalez", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParametrosController_HasAuthorizeAdministradorAttribute()
    {
        var authorizeAttributes = typeof(WebImagenologia.Web.Controllers.ParametrosController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .ToList();

        Assert.Contains(authorizeAttributes, attribute => attribute.Roles == RoleNames.Administrador);
    }

    private async Task<HttpClient> CreateAuthenticatedAdminClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginPage = await client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);
        var serverKey = ExtractServerOptionValue(html);

        var form = new Dictionary<string, string>
        {
            ["Usuario"] = "admin",
            ["Password"] = "secret",
            ["ServidorSeleccionado"] = serverKey,
            ["__RequestVerificationToken"] = token
        };

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        return client;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\"";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "No se encontró el token antiforgery.");

        var valueIndex = html.IndexOf("value=\"", markerIndex, StringComparison.Ordinal) + "value=\"".Length;
        var endIndex = html.IndexOf('"', valueIndex);
        return html[valueIndex..endIndex];
    }

    private static string ExtractServerOptionValue(string html)
    {
        const string marker = "Servidor QA";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "No se encontró la opción de servidor en el HTML.");

        var valueIndex = html.LastIndexOf("value=\"", markerIndex, StringComparison.Ordinal) + "value=\"".Length;
        var endIndex = html.IndexOf('"', valueIndex);
        return html[valueIndex..endIndex];
    }
}

public class ParametrosOperadoresWebApplicationFactory : LoginWebApplicationFactory
{
}
