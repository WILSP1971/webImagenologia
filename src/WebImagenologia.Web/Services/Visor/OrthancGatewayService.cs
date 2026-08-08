using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Orquesta C-MOVE bajo demanda vía REST de Orthanc (SPEC-003).
/// Idempotente: si el estudio ya está en Orthanc (QIDO), no dispara move.
/// </summary>
public sealed class OrthancGatewayService : IOrthancGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly IDicomWebClient _dicomWebClient;
    private readonly VisorOptions _options;
    private readonly ILogger<OrthancGatewayService> _logger;

    public OrthancGatewayService(
        HttpClient httpClient,
        IDicomWebClient dicomWebClient,
        IOptions<VisorOptions> options,
        IConfiguration configuration,
        ILogger<OrthancGatewayService> logger)
    {
        _httpClient = httpClient;
        _dicomWebClient = dicomWebClient;
        _options = options.Value;
        _logger = logger;

        var baseUrl = (_options.OrthancRestBaseUrl ?? "").TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        ApplyBasicAuth(_httpClient, configuration);
    }

    public async Task<bool> AsegurarEstudioDisponibleAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studyInstanceUid))
        {
            return false;
        }

        if (await EstudioYaEnOrthancAsync(studyInstanceUid, cancellationToken))
        {
            return true;
        }

        var modality = string.IsNullOrWhiteSpace(_options.OrthancPacsModality)
            ? "pacs"
            : _options.OrthancPacsModality.Trim();

        var payload = new
        {
            Level = "Study",
            Resources = new[]
            {
                new Dictionary<string, string>
                {
                    ["StudyInstanceUID"] = studyInstanceUid.Trim()
                }
            },
            TargetAet = _options.OrthancAet,
            Synchronous = false
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.PostAsync(
                $"modalities/{Uri.EscapeDataString(modality)}/move",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "C-MOVE Orthanc falló ({Status}) modality={Modality}: {Body}",
                    (int)response.StatusCode,
                    modality,
                    Truncate(body, 300));
                return false;
            }

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                if (await EstudioYaEnOrthancAsync(studyInstanceUid, cancellationToken))
                {
                    return true;
                }
            }

            _logger.LogInformation(
                "C-MOVE aceptado para estudio; aún no visible en QIDO tras espera breve.");
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error al solicitar C-MOVE bajo demanda.");
            return false;
        }
    }

    private async Task<bool> EstudioYaEnOrthancAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken)
    {
        try
        {
            var estudios = await _dicomWebClient.BuscarPorStudyInstanceUidAsync(
                studyInstanceUid,
                cancellationToken);
            return estudios.Count > 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "No se pudo verificar presencia local del estudio en Orthanc.");
            return false;
        }
    }

    private static void ApplyBasicAuth(HttpClient client, IConfiguration configuration)
    {
        var user = configuration["Visor:OrthancUser"];
        var password = configuration["Visor:OrthancPassword"];
        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        var bytes = Encoding.ASCII.GetBytes($"{user}:{password ?? ""}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}
