using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Tests;

public class LecturasTests : IClassFixture<LoginWebApplicationFactory>
{
    private readonly LoginWebApplicationFactory _factory;

    public LecturasTests(LoginWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_AsOperador_ReturnsOkWithFilters()
    {
        _factory.Reset();
        ConfigureOperadorSession();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Operador);
        var response = await client.GetAsync("/Lecturas");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Portal Web Lecturas de Estudios", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Consultar", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Empresa Demo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_AsAdministrador_ReturnsOk()
    {
        _factory.Reset();
        ConfigureAdministradorSession();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Administrador);
        var response = await client.GetAsync("/Lecturas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Index_AsRadiologo_IsForbidden()
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "dr.perez",
            "Dr. Juan Pérez",
            RoleNames.Radiologo,
            [new EmpresaDto("01", "Empresa Demo")]);

        var client = await CreateAuthenticatedClientAsync(RoleNames.Radiologo);
        var response = await client.GetAsync("/Lecturas");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Home/AccesoDenegado", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Consultar_WithFilters_ReturnsFilteredGrid()
    {
        _factory.Reset();
        ConfigureOperadorSession();
        ConfigureLecturasSampleData();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Operador);
        var token = await GetAntiForgeryTokenAsync(client, "/Lecturas");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var form = new Dictionary<string, string>
        {
            ["EmpresaSeleccionada"] = "01",
            ["FechaInicial"] = hoy.AddDays(-7).ToString("yyyy-MM-dd"),
            ["FechaFinal"] = hoy.ToString("yyyy-MM-dd"),
            ["CedulaMedicoFiltro"] = "",
            ["EstadoFiltro"] = "PEN",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Lecturas/Consultar", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("500123", body, StringComparison.Ordinal);
        Assert.Contains("Dr. Juan P", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Radiograf", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ver Detalle", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Detalle_ReturnsViewWithDiagnosticosAndAudioFlag()
    {
        _factory.Reset();
        ConfigureOperadorSession();
        ConfigureLecturasSampleData();
        _factory.ApiClient.AudioStorage["01:2001"] = ([0x01, 0x02], "audio/mpeg");

        var client = await CreateAuthenticatedClientAsync(RoleNames.Operador);
        var response = await client.GetAsync("/Lecturas/Detalle/2001?empresa=01");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Detalle de Lectura", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Diagn", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Notas M", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prueba", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audioPlayer", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LecturasController_HasAuthorizeAdministradorOperadorAttribute()
    {
        var authorizeAttributes = typeof(WebImagenologia.Web.Controllers.LecturasController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .ToList();

        Assert.Contains(authorizeAttributes, attribute =>
            attribute.Roles == $"{RoleNames.Administrador},{RoleNames.Operador}");
    }

    private void ConfigureOperadorSession()
    {
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "operador.test",
            "Operador Demo",
            RoleNames.Operador,
            [new EmpresaDto("01", "Empresa Demo")]);
    }

    private void ConfigureAdministradorSession()
    {
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "admin.test",
            "Administrador Demo",
            RoleNames.Administrador,
            [new EmpresaDto("01", "Empresa Demo")]);
    }

    private void ConfigureLecturasSampleData()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        _factory.ApiClient.Lecturas =
        [
            new LecturaDto(
                "01",
                2001,
                500123m,
                "1234567890",
                "Dr. Juan Pérez",
                hoy,
                hoy.AddDays(-1),
                "SRV01",
                "Radiografía",
                "PEN",
                true),
            new LecturaDto(
                "01",
                2002,
                500456m,
                "0987654321",
                "Dra. Ana López",
                hoy.AddDays(-10),
                hoy.AddDays(-9),
                "SRV02",
                "Tomografía",
                "LEI",
                false)
        ];
    }

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

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        var page = await client.GetAsync(path);
        var html = await page.Content.ReadAsStringAsync();
        return ExtractAntiForgeryToken(html);
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
