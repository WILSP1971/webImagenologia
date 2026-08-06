using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Web.Services;

public interface IEsculapioApiClient
{
    Task<IEnumerable<ServidorDto>> ObtenerServidoresAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<EmpresaDto>> ObtenerEmpresasAsync(
        string ipConexion,
        string bdConexion,
        string portConexion,
        string usuario,
        string password,
        CancellationToken cancellationToken = default);

    Task<UsuarioConexionDto> ValidarConexionAsync(
        string ipConexion,
        string usuario,
        string password,
        CancellationToken cancellationToken = default);

    Task<DiagnosticoDto> ObtenerDiagnosticoCuentaAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<NotaMedicaDto>> ObtenerNotasMedicasCuentaAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<MedicoDto>> ObtenerMedicosAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<RadiologoRegistradoDto>> ObtenerRadiologosRegistradosAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task RegistrarRadiologoAsync(
        RegistrarRadiologoRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task EliminarRadiologoAsync(
        RegistrarRadiologoRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OperadorDto>> ObtenerOperadoresAsync(
        string empresa,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<OperadorRegistradoDto>> ObtenerOperadoresRegistradosAsync(
        string empresa,
        CancellationToken cancellationToken = default);

    Task RegistrarOperadorAsync(
        RegistrarOperadorRequest request,
        CancellationToken cancellationToken = default);

    Task EliminarOperadorAsync(
        string cedula,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<DependenciaDto>> ObtenerDependenciasAsync(
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ServicioDto>> ObtenerServiciosPorDependenciaAsync(
        string codDependencia,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstudioEmpresaDto>> ObtenerEstudiosEmpresaAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task RegistrarEstudioEmpresaAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task EliminarEstudioEmpresaAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AsignacionMedicoDto>> ObtenerAsignacionesMedicoAsync(
        string empresa,
        CancellationToken cancellationToken = default);

    Task RegistrarAsignacionMedicoAsync(
        RegistrarAsignacionMedicoRequest request,
        CancellationToken cancellationToken = default);

    Task EliminarAsignacionMedicoAsync(
        string empresa,
        string cedulaMedico,
        string codDependencia,
        string codServicio,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AutomatizacionDto>> ObtenerAutomatizacionesAsync(
        CancellationToken cancellationToken = default);

    Task RegistrarAutomatizacionAsync(
        RegistrarAutomatizacionRequest request,
        CancellationToken cancellationToken = default);

    Task ToggleAutomatizacionEstadoAsync(
        ToggleAutomatizacionEstadoRequest request,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstudioProgramadoDto>> ObtenerEstudiosProgramadosAsync(
        string empresa,
        string cedulaMedico,
        DateOnly fecha,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<LecturaDto>> ObtenerLecturasAsync(
        string empresa,
        DateOnly fechaInicial,
        DateOnly fechaFinal,
        string? cedulaMedico,
        string? estado,
        long? consecutivo = null,
        CancellationToken cancellationToken = default);

    Task SubirAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        Stream audioStream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType)> ObtenerAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        CancellationToken cancellationToken = default);

    Task EliminarAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstudioSinResultadoDto>> ObtenerEstudiosSinResultadoAsync(
        string empresa,
        DateOnly fechaInicial,
        DateOnly fechaFinal,
        string? codDependencia,
        string? codServicio,
        CancellationToken cancellationToken = default);
}
