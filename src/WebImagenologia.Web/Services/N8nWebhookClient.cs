using System.Net.Http.Json;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Services;

public class N8nWebhookClient : IN8nWebhookClient
{
    public const string HttpClientName = "N8NClient";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<N8nWebhookClient> _logger;

    public N8nWebhookClient(
        IHttpClientFactory httpClientFactory,
        ILogger<N8nWebhookClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> NotifyScheduleUpdateAsync(
        N8nSchedulePayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            _logger.LogDebug(
                "Notificando schedule N8N: frecuencia={Frecuencia}, hora={Hora}, activo={Activo}",
                payload.Frecuencia,
                payload.Hora,
                payload.Activo);

            var response = await client.PostAsJsonAsync(string.Empty, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Webhook N8N respondió {StatusCode}: {Body}",
                    response.StatusCode,
                    body);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "No fue posible invocar el webhook N8N");
            return false;
        }
    }
}
