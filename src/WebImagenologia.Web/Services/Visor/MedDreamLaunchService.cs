using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Integra MedDream TokenService (POST /{v}/generate) o fallback study query-string (solo lab).
/// </summary>
public sealed class MedDreamLaunchService : IMedDreamLaunchService
{
    private readonly HttpClient _httpClient;
    private readonly VisorOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MedDreamLaunchService> _logger;

    public MedDreamLaunchService(
        HttpClient httpClient,
        IOptions<VisorOptions> options,
        IConfiguration configuration,
        ILogger<MedDreamLaunchService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.MedDreamTokenServiceBaseUrl))
        {
            var baseUrl = _options.MedDreamTokenServiceBaseUrl.TrimEnd('/') + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        ApplyBasicAuth(_httpClient, configuration);
    }

    public async Task<string?> BuildViewerUrlAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default)
    {
        if (!_options.MedDreamEnabled ||
            string.IsNullOrWhiteSpace(_options.MedDreamViewerBaseUrl) ||
            string.IsNullOrWhiteSpace(studyInstanceUid))
        {
            return null;
        }

        var viewerBase = _options.MedDreamViewerBaseUrl.TrimEnd('/');
        var storage = string.IsNullOrWhiteSpace(_options.MedDreamStorageId)
            ? "Orthanc"
            : _options.MedDreamStorageId.Trim();

        if (!string.IsNullOrWhiteSpace(_options.MedDreamTokenServiceBaseUrl))
        {
            var token = await GenerateMedDreamTokenAsync(studyInstanceUid, storage, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return $"{viewerBase}/?token={Uri.EscapeDataString(token)}";
            }

            _logger.LogWarning("MedDream TokenService no devolvió token; se evalúa fallback.");
        }

        if (_options.MedDreamAllowStudyQueryString)
        {
            return $"{viewerBase}/?study={Uri.EscapeDataString(studyInstanceUid.Trim())}" +
                   $"&storage={Uri.EscapeDataString(storage)}";
        }

        return null;
    }

    private async Task<string?> GenerateMedDreamTokenAsync(
        string studyInstanceUid,
        string storage,
        CancellationToken cancellationToken)
    {
        var version = string.IsNullOrWhiteSpace(_options.MedDreamTokenApiVersion)
            ? "v4"
            : _options.MedDreamTokenApiVersion.Trim().Trim('/');

        // Formato compatible con TokenService Softneta (items[].studies.study + storage).
        var body = new
        {
            items = new[]
            {
                new
                {
                    studies = new
                    {
                        study = studyInstanceUid.Trim(),
                        storage
                    }
                }
            },
            permissions = new[] { "SEARCH" }
        };

        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.PostAsync($"{version}/generate", content, cancellationToken);
            var responseText = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MedDream generate falló ({Status}): {Body}",
                    (int)response.StatusCode,
                    responseText.Length > 300 ? responseText[..300] + "…" : responseText);
                return null;
            }

            // Respuesta típica: token en texto plano. Algunas builds envuelven JSON {"token":"..."}.
            if (responseText.StartsWith('{') && responseText.EndsWith('}'))
            {
                using var doc = JsonDocument.Parse(responseText);
                if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                {
                    return tokenProp.GetString();
                }
            }

            return string.IsNullOrWhiteSpace(responseText) ? null : responseText.Trim('"');
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Error llamando MedDream TokenService generate.");
            return null;
        }
    }

    private static void ApplyBasicAuth(HttpClient client, IConfiguration configuration)
    {
        var user = configuration["Visor:MedDreamTokenServiceUser"];
        var password = configuration["Visor:MedDreamTokenServicePassword"];
        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        var bytes = Encoding.ASCII.GetBytes($"{user}:{password ?? ""}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
    }
}
