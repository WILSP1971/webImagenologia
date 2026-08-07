using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <inheritdoc cref="IEstudioResolver"/>
/// <remarks>
/// Estrategia (SPEC-002 §4.5):
/// 1. Si el criterio es "caso": se usa el propio Caso/Cuenta como AccessionNumber candidato
///    (el mapeo definitivo Caso→AccessionNumber vía BD/API clínica se fija con SPEC-001/SPEC-003;
///    en F1 el broker ya contempla ambas rutas para no bloquear el contrato de la API).
///    Se intenta QIDO por AccessionNumber; si no hay match, cae a QIDO por PatientID usando el
///    mismo valor de entrada como identificador de respaldo.
/// 2. Si el criterio es "identificacion": va directo a QIDO por PatientID.
/// TODO(SPEC-003/F0): sustituir el paso "Caso/Cuenta -> AccessionNumber candidato" por la
/// resolución real contra la BD/API clínica cuando esté disponible el mapeo verificado.
/// NOTA: este TODO aplica a AMBAS rutas del criterio "caso", no solo a AccessionNumber — el
/// fallback a PatientID (ver BuscarPorPatientIdAsync(caso!, ...) más abajo) también usa hoy el
/// string crudo de "caso" sin resolución alguna contra BD/API clínica: es un placeholder igual
/// de temporal que el de AccessionNumber, pendiente de la misma resolución en SPEC-003/F0.
/// </remarks>
public sealed class EstudioResolver : IEstudioResolver
{
    public const string CriterioCaso = "caso";
    public const string CriterioIdentificacion = "identificacion";

    private readonly IDicomWebClient _dicomWebClient;

    public EstudioResolver(IDicomWebClient dicomWebClient)
    {
        _dicomWebClient = dicomWebClient;
    }

    public async Task<ResolverResponse> ResolverAsync(
        string? caso,
        string? identificacion,
        CancellationToken cancellationToken = default)
    {
        var tieneCaso = !string.IsNullOrWhiteSpace(caso);
        var tieneIdentificacion = !string.IsNullOrWhiteSpace(identificacion);

        if (tieneCaso == tieneIdentificacion)
        {
            throw new ArgumentException(
                "Debe especificar exactamente uno de los criterios: 'caso' o 'identificacion'.");
        }

        if (tieneIdentificacion)
        {
            var porIdentificacion = await _dicomWebClient.BuscarPorPatientIdAsync(identificacion!, cancellationToken);
            return new ResolverResponse
            {
                CriterioBusqueda = CriterioIdentificacion,
                Estudios = porIdentificacion
            };
        }

        // Criterio "caso": intenta AccessionNumber primero; si no hay match, hace fallback a PatientID.
        var porAccession = await _dicomWebClient.BuscarPorAccessionNumberAsync(caso!, cancellationToken);
        if (porAccession.Count > 0)
        {
            return new ResolverResponse
            {
                CriterioBusqueda = CriterioCaso,
                Estudios = porAccession
            };
        }

        var porPatientIdFallback = await _dicomWebClient.BuscarPorPatientIdAsync(caso!, cancellationToken);
        return new ResolverResponse
        {
            CriterioBusqueda = CriterioCaso,
            Estudios = porPatientIdFallback
        };
    }
}
