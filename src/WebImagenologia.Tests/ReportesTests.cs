using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Tests;

public class ReportesTests : IClassFixture<LoginWebApplicationFactory>
{
    private readonly LoginWebApplicationFactory _factory;

    public ReportesTests(LoginWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_AsAdministrador_ReturnsOkWithFilters()
    {
        _factory.Reset();
        ConfigureAdministradorSession();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Administrador);
        var response = await client.GetAsync("/Reportes");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Consultas y Reportes", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exportar Excel", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Empresa Demo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_AsOperador_IsForbidden()
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;
        _factory.ApiClient.UsuarioValido = new UsuarioConexionDto(
            "operador.test",
            "Operador Demo",
            RoleNames.Operador,
            [new EmpresaDto("01", "Empresa Demo")]);

        var client = await CreateAuthenticatedClientAsync(RoleNames.Operador);
        var response = await client.GetAsync("/Reportes");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Home/AccesoDenegado", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Consultar_WithDetalleReport_ReturnsFilteredGrid()
    {
        _factory.Reset();
        ConfigureAdministradorSession();
        ConfigureReportSampleData();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Administrador);
        var token = await GetAntiForgeryTokenAsync(client, "/Reportes");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var form = new Dictionary<string, string>
        {
            ["EmpresasSeleccionadas"] = "01",
            ["FechaInicial"] = hoy.AddDays(-7).ToString("yyyy-MM-dd"),
            ["FechaFinal"] = hoy.ToString("yyyy-MM-dd"),
            ["CedulaMedico"] = "",
            ["CodServicio"] = "",
            ["CodDependencia"] = "",
            ["Estado"] = "PEN",
            ["TipoReporte"] = "DETALLE",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Reportes/Consultar", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("500123", body, StringComparison.Ordinal);
        Assert.Contains("Dr. Juan P", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Radiograf", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Consultar_WithResumenReport_ReturnsSummaryGrid()
    {
        _factory.Reset();
        ConfigureAdministradorSession();
        ConfigureReportSampleData();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Administrador);
        var token = await GetAntiForgeryTokenAsync(client, "/Reportes");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var form = new Dictionary<string, string>
        {
            ["EmpresasSeleccionadas"] = "01",
            ["FechaInicial"] = hoy.AddDays(-7).ToString("yyyy-MM-dd"),
            ["FechaFinal"] = hoy.ToString("yyyy-MM-dd"),
            ["Estado"] = "TODO",
            ["TipoReporte"] = "RESUMEN",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Reportes/Consultar", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Asignados", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Dr. Juan P", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportarExcel_ReturnsSpreadsheetContentType()
    {
        _factory.Reset();
        ConfigureAdministradorSession();
        ConfigureReportSampleData();

        var client = await CreateAuthenticatedClientAsync(RoleNames.Administrador);
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var url = $"/Reportes/ExportarExcel?empresas=01&fechaInicial={hoy.AddDays(-7):yyyy-MM-dd}&fechaFinal={hoy:yyyy-MM-dd}&estado=TODO&tipoReporte=DETALLE";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void ReportesController_HasAuthorizeAdministradorAttribute()
    {
        var authorizeAttributes = typeof(WebImagenologia.Web.Controllers.ReportesController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .ToList();

        Assert.Contains(authorizeAttributes, attribute => attribute.Roles == RoleNames.Administrador);
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

    private void ConfigureReportSampleData()
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
                hoy.AddDays(-2),
                hoy.AddDays(-3),
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
