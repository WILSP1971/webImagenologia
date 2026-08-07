using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Implementación mínima/stub de <see cref="IDicomWebClient"/> para F1 (SPEC-002).
/// TODO(SPEC-003): implementar QIDO-RS/WADO-RS reales contra el gateway Orthanc
/// (<c>VisorOptions.OrthancDicomWebBaseUrl</c>), una vez verificada la cadena
/// Orthanc→dcm4chee en F0. Por ahora no hay red hacia el PACS real desde este entorno,
/// así que este cliente no devuelve resultados (colecciones vacías / null), dejando
/// el contrato listo para que <see cref="EstudioResolver"/> y el resto del broker
/// se inyecten y prueben con mocks.
/// </summary>
public sealed class DicomWebClient : IDicomWebClient
{
    private readonly HttpClient _httpClient;

    public DicomWebClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorAccessionNumberAsync(
        string accessionNumber,
        CancellationToken cancellationToken = default)
    {
        // TODO(SPEC-003): GET {OrthancDicomWebBaseUrl}/studies?AccessionNumber={accessionNumber}
        return Task.FromResult<IReadOnlyList<EstudioDicomDto>>(Array.Empty<EstudioDicomDto>());
    }

    public Task<IReadOnlyList<EstudioDicomDto>> BuscarPorPatientIdAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        // TODO(SPEC-003): GET {OrthancDicomWebBaseUrl}/studies?PatientID={patientId}
        return Task.FromResult<IReadOnlyList<EstudioDicomDto>>(Array.Empty<EstudioDicomDto>());
    }

    public Task<byte[]?> ObtenerRenderedInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        int? frame,
        string formato,
        CancellationToken cancellationToken = default)
    {
        // TODO(SPEC-003): GET {OrthancDicomWebBaseUrl}/studies/{studyInstanceUid}/series/{seriesInstanceUid}
        //                 /instances/{sopInstanceUid}/frames/{frame}/rendered (WADO-RS rendered).
        return Task.FromResult<byte[]?>(null);
    }
}
