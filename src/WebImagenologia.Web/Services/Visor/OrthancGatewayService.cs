namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Implementación mínima/stub de <see cref="IOrthancGatewayService"/> para F1 (SPEC-002).
/// TODO(SPEC-003): implementar C-FIND/C-MOVE reales contra Orthanc (API REST
/// <c>VisorOptions.OrthancRestBaseUrl</c>) una vez verificada en F0 la ruta de red
/// PACS→Orthanc:4242 y el AET <c>VisorOptions.OrthancAet</c> registrado en dcm4chee.
/// </summary>
public sealed class OrthancGatewayService : IOrthancGatewayService
{
    private readonly HttpClient _httpClient;

    public OrthancGatewayService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<bool> AsegurarEstudioDisponibleAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default)
    {
        // TODO(SPEC-003): POST {OrthancRestBaseUrl}/modalities/{OrthancAet}/move (C-MOVE bajo demanda).
        return Task.FromResult(false);
    }
}
