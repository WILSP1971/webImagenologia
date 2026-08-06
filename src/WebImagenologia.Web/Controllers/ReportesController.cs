using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.ViewModels;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Web.Controllers;

[Authorize(Roles = RoleNames.Administrador)]
public class ReportesController : Controller
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IEsculapioApiClient _apiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(
        IEsculapioApiClient apiClient,
        ISessionService sessionService,
        ILogger<ReportesController> logger)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(new ReportesViewModel(), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consultar(ReportesViewModel model, CancellationToken cancellationToken)
    {
        BindDateFilters(model);
        BindEmpresasSeleccionadas(model);
        await PopulateListsAsync(model, cancellationToken);

        if (model.EmpresasSeleccionadas.Count == 0)
        {
            ModelState.AddModelError(nameof(model.EmpresasSeleccionadas), "Debe seleccionar al menos una empresa.");
            return View("Index", model);
        }

        if (model.FechaFinal < model.FechaInicial)
        {
            ModelState.AddModelError(nameof(model.FechaFinal), "La fecha final no puede ser anterior a la fecha inicial.");
            return View("Index", model);
        }

        try
        {
            await LoadReportDataAsync(model, cancellationToken);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible consultar reportes");
            ViewData["ErrorMessage"] = "No fue posible consultar el reporte. Intente nuevamente.";
        }

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(
        [FromQuery] List<string> empresas,
        [FromQuery] string? cedulaMedico,
        [FromQuery] string? codServicio,
        [FromQuery] string? codDependencia,
        [FromQuery] DateOnly fechaInicial,
        [FromQuery] DateOnly fechaFinal,
        [FromQuery] string estado = "TODO",
        [FromQuery] string tipoReporte = "DETALLE",
        CancellationToken cancellationToken = default)
    {
        if (empresas.Count == 0)
        {
            return BadRequest("Debe seleccionar al menos una empresa.");
        }

        if (fechaFinal < fechaInicial)
        {
            return BadRequest("La fecha final no puede ser anterior a la fecha inicial.");
        }

        var model = new ReportesViewModel
        {
            EmpresasSeleccionadas = empresas,
            CedulaMedico = cedulaMedico ?? string.Empty,
            CodServicio = codServicio ?? string.Empty,
            CodDependencia = codDependencia ?? string.Empty,
            FechaInicial = fechaInicial,
            FechaFinal = fechaFinal,
            Estado = estado,
            TipoReporte = tipoReporte
        };

        try
        {
            await LoadReportDataAsync(model, cancellationToken);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible exportar reporte a Excel");
            return StatusCode(502, "No fue posible generar el archivo Excel.");
        }

        var bytes = BuildExcelWorkbook(model);
        var fileName = $"reporte_{tipoReporte.ToLowerInvariant()}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, ExcelContentType, fileName);
    }

    [HttpGet]
    public async Task<IActionResult> MedicosPorEmpresa(string empresa, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa))
        {
            return Json(Array.Empty<object>());
        }

        try
        {
            var medicos = await LoadMedicosPorEmpresaAsync(empresa, cancellationToken);
            var payload = medicos.Select(m => new { m.Cedula, m.Nombre });
            return Json(payload);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar médicos para empresa {Empresa}", empresa);
            return Json(Array.Empty<object>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> ServiciosPorDependencia(
        string codDependencia,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codDependencia))
        {
            return Json(Array.Empty<object>());
        }

        try
        {
            var servicios = await _apiClient.ObtenerServiciosPorDependenciaAsync(codDependencia, cancellationToken);
            var payload = servicios.Select(s => new
            {
                s.CodServicio,
                s.NombreServicio
            });

            return Json(payload);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar servicios para dependencia {Dependencia}", codDependencia);
            return Json(Array.Empty<object>());
        }
    }

    private async Task LoadReportDataAsync(ReportesViewModel model, CancellationToken cancellationToken)
    {
        model.DetalleEstudios = [];
        model.ResumenRadiologos = [];
        model.EstudiosSinResultado = [];

        switch (model.TipoReporte.ToUpperInvariant())
        {
            case "RESUMEN":
                model.ResumenRadiologos = await LoadResumenRadiologosAsync(model, cancellationToken);
                break;
            case "SIN_RESULTADO":
                model.EstudiosSinResultado = await LoadEstudiosSinResultadoAsync(model, cancellationToken);
                break;
            case "PROGRAMACION":
            case "DETALLE":
            default:
                model.DetalleEstudios = await LoadDetalleEstudiosAsync(model, cancellationToken);
                break;
        }
    }

    private async Task<List<ReporteDetalleDto>> LoadDetalleEstudiosAsync(
        ReportesViewModel model,
        CancellationToken cancellationToken)
    {
        var dependencias = await _apiClient.ObtenerDependenciasAsync(cancellationToken: cancellationToken);
        var dependenciaLookup = dependencias.ToDictionary(
            d => d.CodDependencia,
            d => d.NombreDependencia,
            StringComparer.OrdinalIgnoreCase);

        var serviciosLookup = await BuildServiciosLookupAsync(cancellationToken);
        var estado = NormalizeEstadoFilter(model.Estado);
        var result = new List<ReporteDetalleDto>();

        foreach (var empresa in model.EmpresasSeleccionadas)
        {
            var lecturas = await _apiClient.ObtenerLecturasAsync(
                empresa,
                model.FechaInicial,
                model.FechaFinal,
                string.IsNullOrWhiteSpace(model.CedulaMedico) ? null : model.CedulaMedico,
                estado,
                cancellationToken: cancellationToken);

            foreach (var lectura in lecturas)
            {
                if (!MatchesServicioFilter(lectura.CodServicio, model.CodServicio))
                {
                    continue;
                }

                if (!MatchesDependenciaFilter(lectura.CodServicio, model.CodDependencia, serviciosLookup))
                {
                    continue;
                }

                var nombreDependencia = ResolveDependenciaNombre(lectura.CodServicio, serviciosLookup, dependenciaLookup);

                result.Add(new ReporteDetalleDto(
                    lectura.Empresa,
                    lectura.Consecutivo,
                    lectura.NoCuenta,
                    lectura.NombreMedico,
                    lectura.NombreServicio,
                    nombreDependencia,
                    lectura.FechaProgramacion,
                    lectura.Estado,
                    lectura.TieneAudio));
            }
        }

        return result
            .OrderByDescending(r => r.FechaProgramacion)
            .ThenBy(r => r.NoCuenta)
            .ToList();
    }

    private async Task<List<ResumenRadiologoDto>> LoadResumenRadiologosAsync(
        ReportesViewModel model,
        CancellationToken cancellationToken)
    {
        var serviciosLookup = await BuildServiciosLookupAsync(cancellationToken);
        var estado = NormalizeEstadoFilter(model.Estado);
        var lecturas = new List<LecturaDto>();

        foreach (var empresa in model.EmpresasSeleccionadas)
        {
            var empresaLecturas = await _apiClient.ObtenerLecturasAsync(
                empresa,
                model.FechaInicial,
                model.FechaFinal,
                string.IsNullOrWhiteSpace(model.CedulaMedico) ? null : model.CedulaMedico,
                estado,
                cancellationToken: cancellationToken);

            lecturas.AddRange(empresaLecturas.Where(l =>
                MatchesServicioFilter(l.CodServicio, model.CodServicio)
                && MatchesDependenciaFilter(l.CodServicio, model.CodDependencia, serviciosLookup)));
        }

        return lecturas
            .GroupBy(l => l.CedulaMedico, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var totalAsignados = group.Count();
                var totalLeidos = group.Count(l => IsEstadoLeido(l.Estado));
                return new ResumenRadiologoDto(
                    group.Key,
                    group.First().NombreMedico,
                    totalAsignados,
                    totalLeidos,
                    totalAsignados - totalLeidos);
            })
            .OrderBy(r => r.NombreMedico)
            .ToList();
    }

    private async Task<List<EstudioSinResultadoDto>> LoadEstudiosSinResultadoAsync(
        ReportesViewModel model,
        CancellationToken cancellationToken)
    {
        var result = new List<EstudioSinResultadoDto>();
        var codDependencia = string.IsNullOrWhiteSpace(model.CodDependencia) ? null : model.CodDependencia;
        var codServicio = string.IsNullOrWhiteSpace(model.CodServicio) ? null : model.CodServicio;

        foreach (var empresa in model.EmpresasSeleccionadas)
        {
            var estudios = await _apiClient.ObtenerEstudiosSinResultadoAsync(
                empresa,
                model.FechaInicial,
                model.FechaFinal,
                codDependencia,
                codServicio,
                cancellationToken);

            result.AddRange(estudios);
        }

        return result
            .OrderByDescending(e => e.FechaOrden)
            .ThenBy(e => e.NoCuenta)
            .ToList();
    }

    private static byte[] BuildExcelWorkbook(ReportesViewModel model)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Reporte");

        switch (model.TipoReporte.ToUpperInvariant())
        {
            case "RESUMEN":
                WriteResumenSheet(worksheet, model.ResumenRadiologos);
                break;
            case "SIN_RESULTADO":
                WriteSinResultadoSheet(worksheet, model.EstudiosSinResultado);
                break;
            default:
                WriteDetalleSheet(worksheet, model.DetalleEstudios);
                break;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteDetalleSheet(IXLWorksheet worksheet, IReadOnlyList<ReporteDetalleDto> rows)
    {
        worksheet.Cell(1, 1).Value = "Empresa";
        worksheet.Cell(1, 2).Value = "Consecutivo";
        worksheet.Cell(1, 3).Value = "No. Cuenta";
        worksheet.Cell(1, 4).Value = "Médico";
        worksheet.Cell(1, 5).Value = "Servicio";
        worksheet.Cell(1, 6).Value = "Dependencia";
        worksheet.Cell(1, 7).Value = "Fecha Programación";
        worksheet.Cell(1, 8).Value = "Estado";
        worksheet.Cell(1, 9).Value = "Tiene Audio";
        worksheet.Range(1, 1, 1, 9).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = row.Empresa;
            worksheet.Cell(rowIndex, 2).Value = row.Consecutivo;
            worksheet.Cell(rowIndex, 3).Value = row.NoCuenta;
            worksheet.Cell(rowIndex, 4).Value = row.NombreMedico;
            worksheet.Cell(rowIndex, 5).Value = row.NombreServicio;
            worksheet.Cell(rowIndex, 6).Value = row.NombreDependencia;
            worksheet.Cell(rowIndex, 7).Value = row.FechaProgramacion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            worksheet.Cell(rowIndex, 8).Value = FormatEstado(row.Estado);
            worksheet.Cell(rowIndex, 9).Value = row.TieneAudio ? "Sí" : "No";
            rowIndex++;
        }
    }

    private static void WriteResumenSheet(IXLWorksheet worksheet, IReadOnlyList<ResumenRadiologoDto> rows)
    {
        worksheet.Cell(1, 1).Value = "Cédula";
        worksheet.Cell(1, 2).Value = "Radiólogo";
        worksheet.Cell(1, 3).Value = "Total Asignados";
        worksheet.Cell(1, 4).Value = "Total Leídos";
        worksheet.Cell(1, 5).Value = "Total Pendientes";
        worksheet.Range(1, 1, 1, 5).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = row.CedulaMedico;
            worksheet.Cell(rowIndex, 2).Value = row.NombreMedico;
            worksheet.Cell(rowIndex, 3).Value = row.TotalAsignados;
            worksheet.Cell(rowIndex, 4).Value = row.TotalLeidos;
            worksheet.Cell(rowIndex, 5).Value = row.TotalPendientes;
            rowIndex++;
        }
    }

    private static void WriteSinResultadoSheet(IXLWorksheet worksheet, IReadOnlyList<EstudioSinResultadoDto> rows)
    {
        worksheet.Cell(1, 1).Value = "Empresa";
        worksheet.Cell(1, 2).Value = "No. Cuenta";
        worksheet.Cell(1, 3).Value = "No. Orden";
        worksheet.Cell(1, 4).Value = "Servicio";
        worksheet.Cell(1, 5).Value = "Dependencia";
        worksheet.Cell(1, 6).Value = "Fecha Orden";
        worksheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = row.Empresa;
            worksheet.Cell(rowIndex, 2).Value = row.NoCuenta;
            worksheet.Cell(rowIndex, 3).Value = row.NoOrden;
            worksheet.Cell(rowIndex, 4).Value = row.Servicio;
            worksheet.Cell(rowIndex, 5).Value = row.Dependencia;
            worksheet.Cell(rowIndex, 6).Value = row.FechaOrden.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            rowIndex++;
        }
    }

    private async Task<ReportesViewModel> BuildViewModelAsync(
        ReportesViewModel model,
        CancellationToken cancellationToken)
    {
        await PopulateListsAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateListsAsync(ReportesViewModel model, CancellationToken cancellationToken)
    {
        model.EmpresasDisponibles = await LoadEmpresasAsync(cancellationToken);
        model.DependenciasDisponibles = await LoadDependenciasAsync(cancellationToken);

        if (model.EmpresasSeleccionadas.Count == 0 && model.EmpresasDisponibles.Count > 0)
        {
            model.EmpresasSeleccionadas = [model.EmpresasDisponibles[0].Codigo];
        }

        model.MedicosDisponibles = await LoadMedicosForEmpresasAsync(model.EmpresasSeleccionadas, cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.CodDependencia))
        {
            model.ServiciosDisponibles = (await _apiClient.ObtenerServiciosPorDependenciaAsync(
                model.CodDependencia,
                cancellationToken)).ToList();
        }
    }

    private async Task<List<EmpresaDto>> LoadEmpresasAsync(CancellationToken cancellationToken)
    {
        var connection = GetConnectionInfo();
        if (connection is not null)
        {
            try
            {
                var empresas = await _apiClient.ObtenerEmpresasAsync(
                    connection.IpConexion,
                    connection.BdConexion,
                    connection.PortConexion,
                    connection.Usuario,
                    connection.Password,
                    cancellationToken);

                return empresas.ToList();
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No fue posible obtener empresas desde la API; se usan empresas de sesión.");
            }
        }

        return _sessionService.ObtenerUsuario()?.EmpresasAsignadas.ToList() ?? [];
    }

    private async Task<List<DependenciaDto>> LoadDependenciasAsync(CancellationToken cancellationToken)
    {
        try
        {
            return (await _apiClient.ObtenerDependenciasAsync(cancellationToken: cancellationToken)).ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar dependencias");
            return [];
        }
    }

    private async Task<List<MedicoDto>> LoadMedicosForEmpresasAsync(
        IReadOnlyList<string> empresas,
        CancellationToken cancellationToken)
    {
        var medicos = new Dictionary<string, MedicoDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var empresa in empresas)
        {
            var empresaMedicos = await LoadMedicosPorEmpresaAsync(empresa, cancellationToken);
            foreach (var medico in empresaMedicos)
            {
                medicos.TryAdd(medico.Cedula, medico);
            }
        }

        return medicos.Values.OrderBy(m => m.Nombre).ToList();
    }

    private async Task<List<MedicoDto>> LoadMedicosPorEmpresaAsync(
        string empresa,
        CancellationToken cancellationToken)
    {
        try
        {
            var radiologos = await _apiClient.ObtenerRadiologosRegistradosAsync(
                codigoEmpresa: empresa,
                cancellationToken: cancellationToken);
            if (radiologos.Any())
            {
                return radiologos
                    .Select(r => new MedicoDto(r.CedulaMedico, r.NombreMedico))
                    .OrderBy(m => m.Nombre)
                    .ToList();
            }
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogDebug(ex, "No se pudo consultar radiólogos registrados para empresa {Empresa}", empresa);
        }

        try
        {
            var medicos = await _apiClient.ObtenerMedicosAsync(
                codigoEmpresa: empresa,
                cancellationToken: cancellationToken);
            return medicos.OrderBy(m => m.Nombre).ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar médicos para empresa {Empresa}", empresa);
            return [];
        }
    }

    private async Task<Dictionary<string, ServicioDto>> BuildServiciosLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, ServicioDto>(StringComparer.OrdinalIgnoreCase);
        var dependencias = await _apiClient.ObtenerDependenciasAsync(cancellationToken: cancellationToken);

        foreach (var dependencia in dependencias)
        {
            var servicios = await _apiClient.ObtenerServiciosPorDependenciaAsync(
                dependencia.CodDependencia,
                cancellationToken);

            foreach (var servicio in servicios)
            {
                lookup.TryAdd(servicio.CodServicio, servicio);
            }
        }

        return lookup;
    }

    private static bool MatchesServicioFilter(string codServicio, string filter) =>
        string.IsNullOrWhiteSpace(filter)
        || codServicio.Equals(filter, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesDependenciaFilter(
        string codServicio,
        string codDependenciaFilter,
        IReadOnlyDictionary<string, ServicioDto> serviciosLookup)
    {
        if (string.IsNullOrWhiteSpace(codDependenciaFilter))
        {
            return true;
        }

        return serviciosLookup.TryGetValue(codServicio, out var servicio)
            && servicio.CodDependencia.Equals(codDependenciaFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDependenciaNombre(
        string codServicio,
        IReadOnlyDictionary<string, ServicioDto> serviciosLookup,
        IReadOnlyDictionary<string, string> dependenciaLookup)
    {
        if (serviciosLookup.TryGetValue(codServicio, out var servicio)
            && dependenciaLookup.TryGetValue(servicio.CodDependencia, out var nombre))
        {
            return nombre;
        }

        return "-";
    }

    private static string? NormalizeEstadoFilter(string estado) =>
        string.IsNullOrWhiteSpace(estado) || estado.Equals("TODO", StringComparison.OrdinalIgnoreCase)
            ? null
            : estado;

    private static bool IsEstadoLeido(string estado) =>
        estado.Equals("LEI", StringComparison.OrdinalIgnoreCase)
        || estado.Equals("LEIDO", StringComparison.OrdinalIgnoreCase);

    private static string FormatEstado(string estado) =>
        estado.ToUpperInvariant() switch
        {
            "PEN" or "PEND" or "PENDIENTE" => "Pendiente",
            "LEI" or "LEIDO" => "Leído",
            _ => estado
        };

    private ServerConnectionInfo? GetConnectionInfo()
    {
        var connectionJson = _sessionService.ObtenerConnectionString();
        if (string.IsNullOrEmpty(connectionJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ServerConnectionInfo>(connectionJson);
    }

    private void BindDateFilters(ReportesViewModel model)
    {
        if (Request.Form.TryGetValue(nameof(ReportesViewModel.FechaInicial), out var fechaInicialValue)
            && DateOnly.TryParseExact(
                fechaInicialValue.ToString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fechaInicial))
        {
            model.FechaInicial = fechaInicial;
            ModelState.Remove(nameof(ReportesViewModel.FechaInicial));
        }

        if (Request.Form.TryGetValue(nameof(ReportesViewModel.FechaFinal), out var fechaFinalValue)
            && DateOnly.TryParseExact(
                fechaFinalValue.ToString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fechaFinal))
        {
            model.FechaFinal = fechaFinal;
            ModelState.Remove(nameof(ReportesViewModel.FechaFinal));
        }
    }

    private void BindEmpresasSeleccionadas(ReportesViewModel model)
    {
        if (Request.Form.TryGetValue(nameof(ReportesViewModel.EmpresasSeleccionadas), out var empresasValues))
        {
            model.EmpresasSeleccionadas = empresasValues
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ModelState.Remove(nameof(ReportesViewModel.EmpresasSeleccionadas));
        }
    }
}
