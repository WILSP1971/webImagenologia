using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Tests;

public class AsignacionTests : IClassFixture<AsignacionWebApplicationFactory>
{
    private readonly AsignacionWebApplicationFactory _factory;

    public AsignacionTests(AsignacionWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Asignacion_Get_RequiresAuthentication()
    {
        _factory.Reset();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Condicional/Asignacion");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Asignacion_Get_AsAdmin_ReturnsOkWithGridContent()
    {
        _factory.Reset();
        _factory.ApiClient.EstudiosRegistrados.Add(new EstudioEmpresaDto(
            "01",
            "DEP01",
            10,
            "LEC",
            "Imagenología",
            "01 - Empresa Demo"));

        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Condicional/Asignacion");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Asignación de Estudios por Empresa", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Empresa Demo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Asignacion_Registrar_AsAdmin_RedirectsWithSuccess()
    {
        _factory.Reset();
        _factory.ApiClient.EstudiosRegistrados.Add(new EstudioEmpresaDto(
            "01",
            "DEP01",
            10,
            "LEC",
            "Imagenología",
            "01 - Empresa Demo"));

        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Condicional/Asignacion");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["Empresa"] = "01",
            ["CedulaMedico"] = "1234567890",
            ["CodDependencia"] = "DEP01",
            ["CodServicio"] = "SRV01",
            ["Cantidad"] = "3",
            ["Estado"] = "ACT",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Condicional/Asignacion/Registrar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);
        Assert.Single(_factory.ApiClient.AsignacionesRegistradas);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("registrada correctamente", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1234567890", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Asignacion_MedicosPorEmpresa_ReturnsJson()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Condicional/Asignacion/MedicosPorEmpresa?empresa=01");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.True(root.GetArrayLength() >= 1);
        Assert.True(root[0].TryGetProperty("cedula", out _) || root[0].TryGetProperty("Cedula", out _));
    }

    [Fact]
    public async Task Asignacion_DependenciasPorEmpresa_ReturnsJson()
    {
        _factory.Reset();
        _factory.ApiClient.EstudiosRegistrados.Add(new EstudioEmpresaDto(
            "01",
            "DEP01",
            10,
            "LEC",
            "Imagenología",
            "01 - Empresa Demo"));

        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Condicional/Asignacion/DependenciasPorEmpresa?empresa=01");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.True(root.GetArrayLength() >= 1);
        Assert.True(root[0].TryGetProperty("codDependencia", out _) || root[0].TryGetProperty("CodDependencia", out _));
    }

    [Fact]
    public async Task Asignacion_Eliminar_AsAdmin_RedirectsWithSuccess()
    {
        _factory.Reset();
        _factory.ApiClient.AsignacionesRegistradas.Add(new AsignacionMedicoDto(
            "01",
            "1234567890",
            "Dr. Juan Pérez",
            "DEP01",
            "SRV01",
            3,
            "ACT",
            "Imagenología",
            "Radiografía"));

        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Condicional/Asignacion");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["empresa"] = "01",
            ["cedulaMedico"] = "1234567890",
            ["codDependencia"] = "DEP01",
            ["codServicio"] = "SRV01",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Condicional/Asignacion/Eliminar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);
        Assert.Empty(_factory.ApiClient.AsignacionesRegistradas);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("eliminada correctamente", body, StringComparison.OrdinalIgnoreCase);
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

public class AsignacionWebApplicationFactory : LoginWebApplicationFactory
{
}
