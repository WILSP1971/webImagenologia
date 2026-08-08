using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Cliente QIDO-RS / WADO-RS contra Orthanc (SPEC-003 / ADR-002).
/// Base URL: <see cref="VisorOptions.OrthancDicomWebBaseUrl"/> (típicamente .../dicom-web).
/// </summary>
public sealed class DicomWebClient : IDicomWebClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DicomWebClient> _logger;

    public DicomWebClient(
        HttpClient httpClient,
        IOptions<VisorOptions> options,
        IConfiguration configuration,
        ILogger<DicomWebClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var opts = options.Value;
        var baseUrl = (opts.OrthancDicomWebBaseUrl ?? "").TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/dicom+json"));

        ApplyBasicAuth(_httpClient, configuration);
    }

    public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorAccessionNumberAsync(
        string accessionNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessionNumber))
        {
            return Task.FromResult<IReadOnlyList<EstudioDicomDto>>(Array.Empty<EstudioDicomDto>());
        }

        return QueryStudiesAsync(
            $"studies?AccessionNumber={Uri.EscapeDataString(accessionNumber.Trim())}&includefield=all",
            cancellationToken);
    }

    public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorPatientIdAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientId))
        {
            return Task.FromResult<IReadOnlyList<EstudioDicomDto>>(Array.Empty<EstudioDicomDto>());
        }

        return QueryStudiesAsync(
            $"studies?PatientID={Uri.EscapeDataString(patientId.Trim())}&includefield=all",
            cancellationToken);
    }

    public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorStudyInstanceUidAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studyInstanceUid))
        {
            return Task.FromResult<IReadOnlyList<EstudioDicomDto>>(Array.Empty<EstudioDicomDto>());
        }

        return QueryStudiesAsync(
            $"studies?StudyInstanceUID={Uri.EscapeDataString(studyInstanceUid.Trim())}&includefield=all",
            cancellationToken);
    }

    public async Task<byte[]?> ObtenerRenderedInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        int? frame,
        string formato,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studyInstanceUid) ||
            string.IsNullOrWhiteSpace(seriesInstanceUid) ||
            string.IsNullOrWhiteSpace(sopInstanceUid))
        {
            return null;
        }

        var framePart = frame is > 0 ? $"frames/{frame}/" : "";
        var accept = string.Equals(formato, "png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";

        var path =
            $"studies/{Uri.EscapeDataString(studyInstanceUid)}" +
            $"/series/{Uri.EscapeDataString(seriesInstanceUid)}" +
            $"/instances/{Uri.EscapeDataString(sopInstanceUid)}" +
            $"/{framePart}rendered";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "WADO-RS rendered falló ({StatusCode}) para instance {Instance}",
                (int)response.StatusCode,
                sopInstanceUid);
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<EstudioDicomDto>> QueryStudiesAsync(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Array.Empty<EstudioDicomDto>();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "QIDO-RS falló ({StatusCode}) en {Url}",
                    (int)response.StatusCode,
                    relativeUrl);
                return Array.Empty<EstudioDicomDto>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<EstudioDicomDto>();
            }

            var list = new List<EstudioDicomDto>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var mapped = MapStudy(element);
                if (!string.IsNullOrWhiteSpace(mapped.StudyInstanceUID))
                {
                    list.Add(mapped);
                }
            }

            return list;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Error QIDO-RS contra Orthanc ({Url})", relativeUrl);
            throw;
        }
    }

    internal static EstudioDicomDto MapStudy(JsonElement element)
    {
        return new EstudioDicomDto
        {
            StudyInstanceUID = GetStringTag(element, "0020000D") ?? "",
            AccessionNumber = GetStringTag(element, "00080050"),
            PatientId = GetStringTag(element, "00100020"),
            Modality = GetStringTag(element, "00080061") ?? GetStringTag(element, "00080060"),
            StudyDate = GetStringTag(element, "00080020"),
            StudyDescription = GetStringTag(element, "00081030"),
            NumberOfSeries = GetIntTag(element, "00201206"),
            NumberOfInstances = GetIntTag(element, "00201208")
        };
    }

    private static string? GetStringTag(JsonElement element, string tag)
    {
        if (!element.TryGetProperty(tag, out var prop))
        {
            return null;
        }

        if (prop.TryGetProperty("Value", out var values) &&
            values.ValueKind == JsonValueKind.Array &&
            values.GetArrayLength() > 0)
        {
            var first = values[0];
            return first.ValueKind switch
            {
                JsonValueKind.String => first.GetString(),
                JsonValueKind.Number => first.ToString(),
                JsonValueKind.Object when first.TryGetProperty("Alphabetic", out var alpha)
                    => alpha.GetString(),
                _ => first.ToString()
            };
        }

        return null;
    }

    private static int? GetIntTag(JsonElement element, string tag)
    {
        var raw = GetStringTag(element, tag);
        return int.TryParse(raw, out var n) ? n : null;
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
}
