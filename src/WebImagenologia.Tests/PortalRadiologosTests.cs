using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

public class PortalRadiologosTests : IClassFixture<LoginWebApplicationFactory>
{
    private readonly LoginWebApplicationFactory _factory;

    public PortalRadiologosTests(LoginWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("audio/mpeg", true)]
    [InlineData("audio/wav", true)]
    [InlineData("audio/ogg", true)]
    [InlineData("audio/mp4", true)]
    [InlineData("audio/x-m4a", true)]
    [InlineData("audio/flac", true)]
    [InlineData("application/octet-stream", false)]
    [InlineData("application/x-msdownload", false)]
    public void AudioValidation_AllowedContentTypes(string contentType, bool expected)
    {
        Assert.Equal(expected, AudioValidation.IsAllowedContentType(contentType));
    }

    [Theory]
    [InlineData("grabacion.mp3", "application/octet-stream", true)]
    [InlineData("nota.flac", "audio/flac", true)]
    [InlineData("malware.exe", "application/x-msdownload", false)]
    [InlineData("archivo.bin", "application/octet-stream", false)]
    public void AudioValidation_IsAllowedFile(string fileName, string contentType, bool expected)
    {
        Assert.Equal(expected, AudioValidation.IsAllowedFile(fileName, contentType));
    }

    [Fact]
    public void AudioValidation_RejectsFilesLargerThan25Mb()
    {
        const long overLimit = AudioValidation.MaxSizeBytes + 1;
        Assert.False(AudioValidation.IsAllowedSize(overLimit));
        Assert.True(AudioValidation.IsAllowedSize(AudioValidation.MaxSizeBytes));
    }

    [Fact]
    public async Task SubirAudio_ValidMimeType_ReturnsOk()
    {
        _factory.Reset();
        ConfigureRadiologoSession();

        var client = await CreateAuthenticatedRadiologoClientAsync();
        var token = await GetAntiForgeryTokenAsync(client);

        using var content = BuildMultipartAudio(token, "audio.mp3", "audio/mpeg", [0x01, 0x02, 0x03]);

        var response = await client.PostAsync("/PortalRadiologos/SubirAudio", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.ApiClient.AudioStorage.ContainsKey("01:1001"));
    }

    [Fact]
    public async Task SubirAudio_InvalidMimeType_ReturnsBadRequest()
    {
        _factory.Reset();
        ConfigureRadiologoSession();

        var client = await CreateAuthenticatedRadiologoClientAsync();
        var token = await GetAntiForgeryTokenAsync(client);

        using var content = BuildMultipartAudio(token, "malware.exe", "application/x-msdownload", [0x4D, 0x5A]);

        var response = await client.PostAsync("/PortalRadiologos/SubirAudio", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Formato", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permitido", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubirAudio_FileTooLarge_ReturnsBadRequest()
    {
        _factory.Reset();
        ConfigureRadiologoSession();

        var client = await CreateAuthenticatedRadiologoClientAsync();
        var token = await GetAntiForgeryTokenAsync(client);

        var largePayload = new byte[AudioValidation.MaxSizeBytes + 1024];
        using var content = BuildMultipartAudio(token, "large.mp3", "audio/mpeg", largePayload);

        var response = await client.PostAsync("/PortalRadiologos/SubirAudio", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("25 MB", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_AsRadiologo_ReturnsPortalView()
    {
        _factory.Reset();
        ConfigureRadiologoSession();

        var client = await CreateAuthenticatedRadiologoClientAsync();
        var response = await client.GetAsync("/PortalRadiologos");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Portal Web Radiólogos", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Programación de Lecturas", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Subir Audio", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"tabAudio\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audioPlayer", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetalleEstudio_ReturnsJsonWithDiagnosticosAndNotas()
    {
        _factory.Reset();
        ConfigureRadiologoSession();

        var client = await CreateAuthenticatedRadiologoClientAsync();
        var response = await client.GetAsync("/PortalRadiologos/DetalleEstudio?consecutivo=1001&empresa=01");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("estudio", out _));
        Assert.True(json.RootElement.TryGetProperty("diagnosticos", out var diagnosticos));
        Assert.True(json.RootElement.TryGetProperty("notasMedicas", out _));
        Assert.Equal(JsonValueKind.Array, diagnosticos.ValueKind);
    }

    [Fact]
    public async Task Index_AsAdministrador_ReturnsPortalView()
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "admin.test",
            "Administrador Demo",
            RoleNames.Administrador,
            [new EmpresaDto("01", "Empresa Demo")]);

        var client = await CreateAuthenticatedClientAsync(RoleNames.Administrador);
        var response = await client.GetAsync("/PortalRadiologos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Portal Web Radiólogos", html, StringComparison.OrdinalIgnoreCase);
    }

    private void ConfigureRadiologoSession()
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
                DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                "PEND",
                "Paciente Demo",
                false)
        ];
    }

    private async Task<HttpClient> CreateAuthenticatedRadiologoClientAsync() =>
        await CreateAuthenticatedClientAsync(RoleNames.Radiologo);

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string expectedRole)
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

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        var page = await client.GetAsync("/PortalRadiologos");
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
