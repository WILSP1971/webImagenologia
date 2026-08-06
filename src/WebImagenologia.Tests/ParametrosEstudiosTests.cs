using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

public class ParametrosEstudiosTests : IClassFixture<ParametrosEstudiosWebApplicationFactory>
{
    private readonly ParametrosEstudiosWebApplicationFactory _factory;

    public ParametrosEstudiosTests(ParametrosEstudiosWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void EstudioEmpresaApiResponse_MapsCodigoEmpresasAndNombreEmpresa()
    {
        var response = new EstudioEmpresaApiResponse
        {
            CodigoEmpresas = "01",
            NombreEmpresa = "Fundacion Campbell",
            CodDependenciaSnake = "DEP01",
            NombreDependenciaSnake = "Imagenología",
            Cantidad = 5,
            Estado = "LEC"
        };

        var dto = response.ToEstudioEmpresaDto();

        Assert.Equal("01", dto.Empresa);
        Assert.Equal("01 - Fundacion Campbell", dto.NombreEmpresa);
        Assert.Equal("DEP01", dto.CodDependencia);
        Assert.Equal("Imagenología", dto.NombreDependencia);
    }

    [Fact]
    public void DependenciaApiResponse_MapsSnakeCaseFields()
    {
        var response = new DependenciaApiResponse
        {
            CodDependenciaSnake = "DEP99",
            NombreDependenciaSnake = "Urgencias"
        };

        var dto = response.ToDependenciaDto();

        Assert.Equal("DEP99", dto.CodDependencia);
        Assert.Equal("Urgencias", dto.NombreDependencia);
    }

    [Fact]
    public void DependenciaApiResponse_MapsNomDependeciaFromApi()
    {
        const string json = """
            [
              {"$id":"1","CodDependencia":"01","NomDependecia":"RADIOLOGIA"},
              {"$id":"2","CodDependencia":"12","NomDependecia":"TOMOGRAFÍA"}
            ]
            """;

        var dependencias = ApiFlexibleJson.ParseDependencias(json);

        Assert.Equal(2, dependencias.Count);
        Assert.Equal("01", dependencias[0].CodDependencia);
        Assert.Equal("RADIOLOGIA", dependencias[0].NombreDependencia);
        Assert.Equal("TOMOGRAFÍA", dependencias[1].NombreDependencia);
    }

    [Fact]
    public void EmpresaDto_Etiqueta_UsesCodigoAndNombre()
    {
        var empresa = new EmpresaDto("03", "MOVID IPS S.A.S");
        Assert.Equal("03 - MOVID IPS S.A.S", empresa.Etiqueta);
    }

    [Fact]
    public async Task Estudios_Get_RequiresAuthentication()
    {
        _factory.Reset();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Parametros/Estudios");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Estudios_Get_AsAdmin_ReturnsOkWithGridContent()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Parametros/Estudios");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Parametrización de Estudios", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Imagenología", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Empresa Demo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Estudios_Registrar_AsAdmin_RedirectsWithSuccess()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Parametros/Estudios");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["CodDependencia"] = "DEP01",
            ["CodServicio"] = "SRV01",
            ["CodEsquema"] = "ESQ01",
            ["Cantidad"] = "5",
            ["EsLectura"] = "true",
            ["EmpresasSeleccionadas"] = "01",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Parametros/Estudios/Registrar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);
        Assert.Single(_factory.ApiClient.EstudiosRegistrados);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("registrado correctamente", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DEP01", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Estudios_ServiciosPorDependencia_ReturnsJson()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Parametros/Estudios/ServiciosPorDependencia?codDependencia=DEP01");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.True(root.GetArrayLength() >= 1);
        Assert.True(root[0].TryGetProperty("codServicio", out _) || root[0].TryGetProperty("CodServicio", out _));
    }

    [Fact]
    public async Task Estudios_Eliminar_AsAdmin_RedirectsWithSuccess()
    {
        _factory.Reset();
        _factory.ApiClient.EstudiosRegistrados.Add(new EstudioEmpresaDto(
            "01",
            "DEP01",
            5,
            "LEC",
            "Imagenología",
            "01 - Empresa Demo"));

        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Parametros/Estudios");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["empresa"] = "01",
            ["codDependencia"] = "DEP01",
            ["codServicio"] = "SRV01",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Parametros/Estudios/Eliminar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("eliminado correctamente", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Radiografía", body, StringComparison.OrdinalIgnoreCase);
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

public class ParametrosEstudiosWebApplicationFactory : LoginWebApplicationFactory
{
}
