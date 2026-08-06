using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

public class AutomatizacionTests : IClassFixture<AutomatizacionWebApplicationFactory>
{
    private readonly AutomatizacionWebApplicationFactory _factory;

    public AutomatizacionTests(AutomatizacionWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Automatizacion_Get_RequiresAuthentication()
    {
        _factory.Reset();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Condicional/Automatizacion");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Automatizacion_Get_AsAdmin_ReturnsOkWithFormContent()
    {
        _factory.Reset();
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Condicional/Automatizacion");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Automatización Programación de Estudios", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chkRadiologos", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chkOperador", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Automatizacion_Registrar_AsAdmin_InvokesN8nWebhook()
    {
        _factory.Reset();
        _factory.N8nWebhookClient.Reset();

        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Condicional/Automatizacion");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["TipoProgramacion"] = "RAD",
            ["Frecuencia"] = "DIA",
            ["HoraAutomatizacion"] = "06:00",
            ["Activo"] = "true",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Condicional/Automatizacion/Registrar", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);
        Assert.Single(_factory.ApiClient.AutomatizacionesRegistradas);
        Assert.Single(_factory.N8nWebhookClient.Invocations);

        var payload = _factory.N8nWebhookClient.Invocations[0];
        Assert.Equal("DIA", payload.Frecuencia);
        Assert.Equal("06:00", payload.Hora);
        Assert.True(payload.Activo);
        Assert.Equal("RAD", payload.TipoProgramacion);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("registrada correctamente", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Automatizacion_ToggleEstado_AsAdmin_InvokesN8nWebhookWithInactive()
    {
        _factory.Reset();
        _factory.N8nWebhookClient.Reset();
        _factory.ApiClient.AutomatizacionesRegistradas.Add(new AutomatizacionDto(
            1,
            "RAD",
            "DIA",
            "06:00",
            "ACT"));

        var client = await CreateAuthenticatedAdminClientAsync();

        var getPage = await client.GetAsync("/Condicional/Automatizacion");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["idAutomatizacion"] = "1",
            ["activo"] = "false",
            ["frecuencia"] = "DIA",
            ["horaAutomatizacion"] = "06:00",
            ["tipoProgramacion"] = "RAD",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Condicional/Automatizacion/ToggleEstado", new FormUrlEncodedContent(form));
        var redirectResponse = await client.GetAsync(response.Headers.Location);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redirectResponse.StatusCode);
        Assert.Equal("INA", _factory.ApiClient.AutomatizacionesRegistradas[0].Estado);
        Assert.Single(_factory.N8nWebhookClient.Invocations);

        var payload = _factory.N8nWebhookClient.Invocations[0];
        Assert.False(payload.Activo);

        var body = await redirectResponse.Content.ReadAsStringAsync();
        Assert.Contains("desactivada correctamente", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task N8nWebhookClient_WithMockHandler_PostsSchedulePayload()
    {
        N8nSchedulePayload? capturedPayload = null;

        var handler = new CaptureJsonHandler<N8nSchedulePayload>(payload => capturedPayload = payload);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://n8n.test/webhook/actualizar-schedule/")
        };

        var httpClientFactory = new StubHttpClientFactory(
            N8nWebhookClient.HttpClientName,
            httpClient);

        var client = new N8nWebhookClient(httpClientFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<N8nWebhookClient>.Instance);

        var payload = new N8nSchedulePayload("DIA", "06:00", true, "RAD");
        var result = await client.NotifyScheduleUpdateAsync(payload);

        Assert.True(result);
        Assert.NotNull(capturedPayload);
        Assert.Equal("DIA", capturedPayload!.Frecuencia);
        Assert.Equal("06:00", capturedPayload.Hora);
        Assert.True(capturedPayload.Activo);
        Assert.Equal("RAD", capturedPayload.TipoProgramacion);
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

public class AutomatizacionWebApplicationFactory : LoginWebApplicationFactory
{
}

public sealed class FakeN8nWebhookClient : IN8nWebhookClient
{
    public List<N8nSchedulePayload> Invocations { get; } = [];

    public bool ShouldSucceed { get; set; } = true;

    public void Reset()
    {
        Invocations.Clear();
        ShouldSucceed = true;
    }

    public Task<bool> NotifyScheduleUpdateAsync(
        N8nSchedulePayload payload,
        CancellationToken cancellationToken = default)
    {
        Invocations.Add(payload);
        return Task.FromResult(ShouldSucceed);
    }
}

internal sealed class CaptureJsonHandler<T> : HttpMessageHandler
{
    private readonly Action<T> _onCapture;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CaptureJsonHandler(Action<T> onCapture)
    {
        _onCapture = onCapture;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var json = await request.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (payload is not null)
            {
                _onCapture(payload);
            }
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly string _clientName;
    private readonly HttpClient _httpClient;

    public StubHttpClientFactory(string clientName, HttpClient httpClient)
    {
        _clientName = clientName;
        _httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        if (!name.Equals(_clientName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cliente HTTP no configurado: {name}");
        }

        return _httpClient;
    }
}
