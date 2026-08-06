using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

/// <summary>
/// Suite de regresión global (Fase 12): login por rol, acceso denegado y validación de audio.
/// </summary>
public class RegressionTests : IClassFixture<LoginWebApplicationFactory>
{
    private readonly LoginWebApplicationFactory _factory;

    public RegressionTests(LoginWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(RoleNames.Administrador, "admin.test")]
    [InlineData(RoleNames.Radiologo, "dr.perez")]
    [InlineData(RoleNames.Operador, "operador.test")]
    public async Task Login_WithEachRole_RedirectsToRoleHome(string role, string userName)
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            userName,
            userName,
            role,
            [new EmpresaDto("01", "Empresa Demo")]);

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
            ["Usuario"] = userName,
            ["Password"] = "secret",
            ["ServidorSeleccionado"] = serverKey,
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        var redirectOk = role switch
        {
            RoleNames.Radiologo => location.Contains("/PortalRadiologos", StringComparison.OrdinalIgnoreCase),
            RoleNames.Operador => location.Contains("/Lecturas", StringComparison.OrdinalIgnoreCase),
            _ => location.Contains("/Home", StringComparison.OrdinalIgnoreCase) || location == "/",
        };

        Assert.True(redirectOk, $"Redirect inesperado para rol {role}: {location}");

        var landingResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, landingResponse.StatusCode);
    }

    [Theory]
    [InlineData("/Parametros/Radiologos", RoleNames.Operador)]
    [InlineData("/Parametros/Radiologos", RoleNames.Radiologo)]
    [InlineData("/Condicional/Asignacion", RoleNames.Operador)]
    [InlineData("/Reportes", RoleNames.Operador)]
    [InlineData("/Reportes", RoleNames.Radiologo)]
    [InlineData("/PortalRadiologos", RoleNames.Operador)]
    [InlineData("/Lecturas", RoleNames.Radiologo)]
    public async Task ProtectedRoute_WrongRole_RedirectsToAccessDenied(string route, string role)
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "user.test",
            "Usuario Test",
            role,
            [new EmpresaDto("01", "Empresa Demo")]);

        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Home/AccesoDenegado", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/Parametros/Radiologos")]
    [InlineData("/Condicional/Automatizacion")]
    [InlineData("/PortalRadiologos")]
    [InlineData("/Lecturas")]
    [InlineData("/Reportes")]
    public async Task ProtectedRoute_Unauthenticated_RedirectsToLogin(string route)
    {
        _factory.Reset();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AudioUpload_ValidMimeType_Accepted()
    {
        _factory.Reset();
        ConfigureRadiologoForAudio();

        var client = await CreateAuthenticatedClientAsync();
        var token = await GetAntiForgeryTokenAsync(client, "/PortalRadiologos");

        using var content = BuildMultipartAudio(token, "lectura.mp3", "audio/mpeg", [0x01, 0x02, 0x03]);
        var response = await client.PostAsync("/PortalRadiologos/SubirAudio", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.ApiClient.AudioStorage.ContainsKey("01:1001"));
    }

    [Fact]
    public async Task AudioUpload_InvalidMimeType_RejectedWithMessage()
    {
        _factory.Reset();
        ConfigureRadiologoForAudio();

        var client = await CreateAuthenticatedClientAsync();
        var token = await GetAntiForgeryTokenAsync(client, "/PortalRadiologos");

        using var content = BuildMultipartAudio(token, "malware.exe", "application/x-msdownload", [0x4D, 0x5A]);
        var response = await client.PostAsync("/PortalRadiologos/SubirAudio", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("permitido", body, StringComparison.OrdinalIgnoreCase);
        Assert.False(_factory.ApiClient.AudioStorage.ContainsKey("01:1001"));
    }

    private void ConfigureRadiologoForAudio()
    {
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "dr.perez",
            "Dr. Juan Pérez",
            RoleNames.Radiologo,
            [new EmpresaDto("01", "Empresa Demo")]);

        _factory.ApiClient.RadiologosRegistrados =
        [
            new RadiologoRegistradoDto("1234567890", "Dr. Juan Pérez", "dr.perez", "DEP01", "Imagenología", 5, ["01"])
        ];

        _factory.ApiClient.EstudiosProgramados =
        [
            new EstudioProgramadoDto(
                "01",
                1001,
                500123m,
                "1234567890",
                DateOnly.FromDateTime(DateTime.Today),
                "Radiografía",
                "SRV01",
                "Imagenología",
                9001m,
                "operador1",
                DateOnly.FromDateTime(DateTime.Today),
                "PEND",
                "Paciente Demo",
                false)
        ];
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
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
            ["Usuario"] = "user.test",
            ["Password"] = "secret",
            ["ServidorSeleccionado"] = serverKey,
            ["__RequestVerificationToken"] = token
        };

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        return client;
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        var page = await client.GetAsync(path);
        var html = await page.Content.ReadAsStringAsync();
        return ExtractAntiForgeryToken(html);
    }

    private static MultipartFormDataContent BuildMultipartAudio(
        string antiForgeryToken,
        string fileName,
        string contentType,
        byte[] payload)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(payload);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "archivo", fileName);
        content.Add(new StringContent("1001"), "consecutivo");
        content.Add(new StringContent("01"), "empresa");
        content.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
        return content;
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
