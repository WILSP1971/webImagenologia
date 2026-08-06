using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Win32;
using System.Text.Json;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.ViewModels;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Web.Controllers;

[Authorize(Roles = RoleNames.Administrador)]
[Route("[controller]")]
public class ParametrosController : Controller
{
    private readonly IEsculapioApiClient _apiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<ParametrosController> _logger;

    public ParametrosController(
        IEsculapioApiClient apiClient,
        ISessionService sessionService,
        ILogger<ParametrosController> logger)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet("Radiologos")]
    public async Task<IActionResult> Radiologos(CancellationToken cancellationToken)
    {
        var model = await BuildRadiologosViewModelAsync(cancellationToken);
        ApplyTempDataMessages();
        return View(model);
    }

    [HttpPost("Radiologos/Registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RadiologosViewModel model, CancellationToken cancellationToken)
    {
        if (model.EmpresasSeleccionadas.Count == 0)
        {
            ModelState.AddModelError(nameof(model.EmpresasSeleccionadas), "Seleccione al menos una empresa.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateRadiologosListsAsync(model, cancellationToken);
            return View("Radiologos", model);
        }

        await PopulateRadiologosListsAsync(model, cancellationToken);

        var medico = model.MedicosDisponibles.FirstOrDefault(m =>
            m.Cedula.Equals(model.CedulaMedico, StringComparison.OrdinalIgnoreCase));

        if (medico is null)
        {
            ModelState.AddModelError(nameof(model.CedulaMedico), "El médico seleccionado no es válido.");
            return View("Radiologos", model);
        }

        var nombreMedico = string.IsNullOrWhiteSpace(model.NombreMedico)
            ? medico.Nombre
            : model.NombreMedico;

        var estado = model.EsLectura? "A" : "I";
        var tipo = model.ModoEdicion ? "U" : "I";
        var connection = GetConnectionInfo();
        try
        {
            if (model.DependenciasDisponibles.Count > 0)
            {
                var dependencia = model.DependenciasDisponibles.FirstOrDefault(d =>
                    d.CodDependencia.Equals(model.CodDependencia, StringComparison.OrdinalIgnoreCase));

                if (dependencia is null)
                {
                    ModelState.AddModelError(nameof(model.CodDependencia), "La dependencia seleccionada no es válida.");
                    return View("Radiologos", model);
                }

                if (string.IsNullOrWhiteSpace(model.NombreDependencia))
                {
                    model.NombreDependencia = dependencia.NombreDependencia;
                }
            }

            var empresas = model.ModoEdicion
            ? [model.EmpresaEdicion]
            : model.EmpresasSeleccionadas;

            foreach (var empresa in empresas)
            {
                var request = new RegistrarRadiologoRequest(
                    empresa,
                    model.CedulaMedico,
                    model.UsuarioEsculapio,
                    model.CodDependencia,
                    model.Cantidad,
                    estado,
                    tipo);

                await _apiClient.RegistrarRadiologoAsync(request, connection, cancellationToken);
            }

            TempData["SuccessMessage"] = "Radiólogo registrado correctamente.";
            return RedirectToAction(nameof(Radiologos));
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al registrar radiólogo {Cedula}", model.CedulaMedico);
            ModelState.AddModelError(string.Empty, "No fue posible registrar el radiólogo. Intente nuevamente.");
            return View("Radiologos", model);
        }
    }

    [HttpGet("Radiologos/Editar/{cedula}")]
    public async Task<IActionResult> Editar(string cedula, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return RedirectToAction(nameof(Radiologos));
        }

        var model = await BuildRadiologosViewModelAsync(cancellationToken);
        var radiologo = model.RadiologosRegistrados.FirstOrDefault(r =>
            r.CedulaMedico.Equals(cedula, StringComparison.OrdinalIgnoreCase));

        if (radiologo is null)
        {
            TempData["ErrorMessage"] = "No se encontró el radiólogo solicitado.";
            return RedirectToAction(nameof(Radiologos));
        }

        model.CedulaMedico = radiologo.CedulaMedico;
        model.NombreMedico = radiologo.NombreMedico;
        model.UsuarioEsculapio = radiologo.UsuarioEsculapio;
        model.CodDependencia = radiologo.CodDependencia;
        model.NombreDependencia = radiologo.NombreDependencia;
        model.Cantidad = radiologo.Cantidad;
        model.EmpresasSeleccionadas = radiologo.Empresas.ToList();
        model.ModoEdicion = true;

        return View("RadiologosEditar", model);
    }


    [HttpPost("Radiologos/Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(
        string empresa,
        string cedulaMedico,
        string codDependencia,
        string usuarioEsculapio,
        decimal cantidad,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa)
            || string.IsNullOrWhiteSpace(cedulaMedico)
            || string.IsNullOrWhiteSpace(codDependencia))
        {
            TempData["ErrorMessage"] = "Los datos del radiólogo son requeridos.";
            return RedirectToAction(nameof(Radiologos));
        }

        var connection = GetConnectionInfo();

        try
        {
            var request = new RegistrarRadiologoRequest(
                empresa,
                cedulaMedico,
                usuarioEsculapio ?? string.Empty,
                codDependencia,
                cantidad,
                "I",
                "I");

            await _apiClient.EliminarRadiologoAsync(request, connection, cancellationToken);
            TempData["SuccessMessage"] = "Radiólogo eliminado correctamente.";
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al eliminar radiólogo {Cedula}", cedulaMedico);
            TempData["ErrorMessage"] = "No fue posible eliminar el radiólogo. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Radiologos));
    }


    [HttpGet("Operadores")]
    public async Task<IActionResult> Operadores(CancellationToken cancellationToken)
    {
        var model = await BuildOperadoresViewModelAsync(cancellationToken);
        ApplyTempDataMessages();
        return View(model);
    }

    [HttpPost("Operadores/Registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarOperador(OperadoresViewModel model, CancellationToken cancellationToken)
    {
        if (model.EmpresasSeleccionadas.Count == 0)
        {
            ModelState.AddModelError(nameof(model.EmpresasSeleccionadas), "Seleccione al menos una empresa.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOperadoresListsAsync(model, cancellationToken);
            return View(model.ModoEdicion ? "OperadoresEditar" : "Operadores", model);
        }

        await PopulateOperadoresListsAsync(model, cancellationToken);

        var operador = model.OperadoresDisponibles.FirstOrDefault(o =>
            o.Cedula.Equals(model.CedulaOperador, StringComparison.OrdinalIgnoreCase));

        if (operador is null)
        {
            ModelState.AddModelError(nameof(model.CedulaOperador), "El operador seleccionado no es válido.");
            return View(model.ModoEdicion ? "OperadoresEditar" : "Operadores", model);
        }

        var nombreOperador = string.IsNullOrWhiteSpace(model.NombreOperador)
            ? operador.Nombre
            : model.NombreOperador;

        try
        {
            var request = new RegistrarOperadorRequest(
                model.CedulaOperador,
                nombreOperador,
                model.UsuarioEsculapio,
                model.EmpresasSeleccionadas);

            await _apiClient.RegistrarOperadorAsync(request, cancellationToken);
            TempData["SuccessMessage"] = "Operador registrado correctamente.";
            return RedirectToAction(nameof(Operadores));
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al registrar operador {Cedula}", model.CedulaOperador);
            ModelState.AddModelError(string.Empty, "No fue posible registrar el operador. Intente nuevamente.");
            return View(model.ModoEdicion ? "OperadoresEditar" : "Operadores", model);
        }
    }

    [HttpGet("Operadores/Editar/{cedula}")]
    public async Task<IActionResult> EditarOperador(string cedula, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return RedirectToAction(nameof(Operadores));
        }

        var model = await BuildOperadoresViewModelAsync(cancellationToken);
        var operador = model.OperadoresRegistrados.FirstOrDefault(o =>
            o.CedulaOperador.Equals(cedula, StringComparison.OrdinalIgnoreCase));

        if (operador is null)
        {
            TempData["ErrorMessage"] = "No se encontró el operador solicitado.";
            return RedirectToAction(nameof(Operadores));
        }

        model.CedulaOperador = operador.CedulaOperador;
        model.NombreOperador = operador.NombreOperador;
        model.UsuarioEsculapio = operador.UsuarioEsculapio;
        model.EmpresasSeleccionadas = operador.Empresas.ToList();
        model.ModoEdicion = true;

        return View("OperadoresEditar", model);
    }

    [HttpPost("Operadores/Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarOperador(string cedula, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            TempData["ErrorMessage"] = "La cédula del operador es requerida.";
            return RedirectToAction(nameof(Operadores));
        }

        try
        {
            await _apiClient.EliminarOperadorAsync(cedula, cancellationToken);
            TempData["SuccessMessage"] = "Operador eliminado correctamente.";
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al eliminar operador {Cedula}", cedula);
            TempData["ErrorMessage"] = "No fue posible eliminar el operador. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Operadores));
    }

    [HttpGet("Estudios")]
    public async Task<IActionResult> Estudios(CancellationToken cancellationToken)
    {
        var model = await BuildEstudiosViewModelAsync(cancellationToken);
        ApplyTempDataMessages();

        if (model.DependenciasDisponibles.Count == 0)
        {
            ViewData["WarningMessage"] =
                "No se cargaron dependencias desde la API. Verifique la conexión al servidor Esculapio.";
        }

        return View(model);
    }

    [HttpGet("Estudios/ServiciosPorDependencia")]
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
                s.NombreServicio,
                s.CodEsquema
            });

            return Json(payload);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar servicios para dependencia {CodDependencia}", codDependencia);
            return Json(Array.Empty<object>());
        }
    }

    [HttpPost("Estudios/Registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarEstudio(EstudiosViewModel model, CancellationToken cancellationToken)
    {
        if (model.EmpresasSeleccionadas.Count == 0)
        {
            ModelState.AddModelError(nameof(model.EmpresasSeleccionadas), "Seleccione al menos una empresa.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEstudiosListsAsync(model, cancellationToken);
            return View(model.ModoEdicion ? "EstudiosEditar" : "Estudios", model);
        }

        await PopulateEstudiosListsAsync(model, cancellationToken);

        // Solo validar la existencia en catálogo cuando la lista se cargó correctamente.
        // Si la API de dependencias no responde, dejamos pasar con el código recibido del form.
        if (model.DependenciasDisponibles.Count > 0)
        {
            var Dependencia = model.DependenciasDisponibles.FirstOrDefault(d =>
                d.CodDependencia.Equals(model.CodDependencia, StringComparison.OrdinalIgnoreCase));

            if (Dependencia is null)
            {
                ModelState.AddModelError(nameof(model.CodDependencia), "La dependencia seleccionada no es válida.");
                return View(model.ModoEdicion ? "EstudiosEditar" : "Estudios", model);
            }

            // Enriquecer nombre si el cliente no lo envió
            if (string.IsNullOrWhiteSpace(model.NombreDependencia))
            {
                model.NombreDependencia = Dependencia.NombreDependencia;
            }
        }

        // Estado = "A" siempre (registro activo). El SP usa "A"/"I" para activo/inactivo del registro,
        // no para el indicador de Lectura. EsLectura se representa mediante el Tipo de servicio.
        const string estado = "A";
        var tipo = model.ModoEdicion ? "U" : "I";
        var connection = GetConnectionInfo();

        if (connection is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "No hay datos de conexión en la sesión. Cierre sesión e ingrese nuevamente.");
            return View(model.ModoEdicion ? "EstudiosEditar" : "Estudios", model);
        }

        try
        {
            var empresas = model.ModoEdicion
                ? [model.EmpresaEdicion]
                : model.EmpresasSeleccionadas;

            foreach (var empresa in empresas)
            {
                var request = new RegistrarEstudioEmpresaRequest(
                    empresa,
                    model.CodDependencia,
                    model.Cantidad,
                    estado,
                    tipo);

                await _apiClient.RegistrarEstudioEmpresaAsync(request, connection, cancellationToken);
            }

            TempData["SuccessMessage"] = model.ModoEdicion
                ? "Estudio actualizado correctamente."
                : "Estudio registrado correctamente.";
            return RedirectToAction(nameof(Estudios));
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al registrar estudio {CodDependencia}", model.CodDependencia);
            ModelState.AddModelError(string.Empty, "No fue posible registrar el estudio. Intente nuevamente.");
            return View(model.ModoEdicion ? "EstudiosEditar" : "Estudios", model);
        }
    }

    [HttpGet("Estudios/Editar")]
    public async Task<IActionResult> EditarEstudio(
        string empresa,
        string codDependencia,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa)
            || string.IsNullOrWhiteSpace(codDependencia))
        {
            return RedirectToAction(nameof(Estudios));
        }

        var model = await BuildEstudiosViewModelAsync(cancellationToken);
        var estudio = model.EstudiosRegistrados.FirstOrDefault(e =>
            e.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
            && e.CodDependencia.Equals(codDependencia, StringComparison.OrdinalIgnoreCase));

        if (estudio is null)
        {
            TempData["ErrorMessage"] = "No se encontró el estudio solicitado.";
            return RedirectToAction(nameof(Estudios));
        }

        model.CodDependencia = estudio.CodDependencia;
        model.NombreDependencia = estudio.NombreDependencia;
        model.Cantidad = estudio.Cantidad;
        model.EsLectura = estudio.Estado.Equals("A", StringComparison.OrdinalIgnoreCase);
        model.EmpresasSeleccionadas = [estudio.Empresa];
        model.EmpresaEdicion = estudio.Empresa;
        model.NombreEmpresa = estudio.NombreEmpresa;
        model.ModoEdicion = true;

        return View("EstudiosEditar", model);
    }

    [HttpPost("Estudios/Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEstudio(
        string empresa,
        string codDependencia,
        string? codServicio,
        decimal cantidad = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(empresa) || string.IsNullOrWhiteSpace(codDependencia))
        {
            TempData["ErrorMessage"] = "Los datos del estudio son requeridos.";
            return RedirectToAction(nameof(Estudios));
        }

        var connection = GetConnectionInfo();

        try
        {
            var request = new RegistrarEstudioEmpresaRequest(
                empresa,
                codDependencia,
                cantidad > 0 ? cantidad : 1,
                "I",
                "I");

            await _apiClient.EliminarEstudioEmpresaAsync(request, connection, cancellationToken);
            TempData["SuccessMessage"] = "Estudio eliminado correctamente.";
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al eliminar estudio {Empresa}/{CodDependencia}", empresa, codDependencia);
            TempData["ErrorMessage"] = "No fue posible eliminar el estudio. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Estudios));
    }

    private async Task<RadiologosViewModel> BuildRadiologosViewModelAsync(CancellationToken cancellationToken)
    {
        var model = new RadiologosViewModel();
        await PopulateRadiologosListsAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateRadiologosListsAsync(RadiologosViewModel model, CancellationToken cancellationToken)
    {
        var connection = GetConnectionInfo();
        model.EmpresasDisponibles = await LoadEmpresasAsync(cancellationToken);
        model.MedicosDisponibles = await LoadMedicosAsync(connection, cancellationToken);

        try
        {
            model.DependenciasDisponibles = (await _apiClient.ObtenerDependenciasAsync(connection, cancellationToken)).ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar dependencias desde la API.");
            model.DependenciasDisponibles = [];
        }

        if (model.MedicosDisponibles.Count == 0)
        {
            ViewData["WarningMessage"] = "No se obtuvieron médicos desde la API (Imagenologo/obtener-readiologos). Verifique la conexión del servidor.";
        }

        model.RadiologosRegistrados = await LoadRadiologosRegistradosAsync(
            model.EmpresasDisponibles,
            connection,
            cancellationToken);
    }

    private async Task<OperadoresViewModel> BuildOperadoresViewModelAsync(CancellationToken cancellationToken)
    {
        var model = new OperadoresViewModel();
        await PopulateOperadoresListsAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateOperadoresListsAsync(OperadoresViewModel model, CancellationToken cancellationToken)
    {
        model.EmpresasDisponibles = await LoadEmpresasAsync(cancellationToken);
        model.OperadoresDisponibles = await LoadOperadoresAsync(model.EmpresasDisponibles, cancellationToken);
        model.OperadoresRegistrados = await LoadOperadoresRegistradosAsync(model.EmpresasDisponibles, cancellationToken);
    }

    private async Task<List<EmpresaDto>> LoadEmpresasAsync(CancellationToken cancellationToken)
    {
        var sessionEmpresas = _sessionService.ObtenerUsuario()?.EmpresasAsignadas.ToList() ?? [];
        if (sessionEmpresas.Count > 0)
        {
            return DedupeEmpresas(sessionEmpresas);
        }

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

                return DedupeEmpresas(empresas);
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No fue posible obtener empresas desde la API.");
            }
        }

        return [];
    }

    private static List<EmpresaDto> DedupeEmpresas(IEnumerable<EmpresaDto> empresas) =>
        empresas
            .Where(e => !string.IsNullOrWhiteSpace(e.Codigo))
            .GroupBy(e => e.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(e => e.Nombre, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<List<MedicoDto>> LoadMedicosAsync(
        ServerConnectionInfo? connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var medicos = await _apiClient.ObtenerMedicosAsync(connection: connection, cancellationToken: cancellationToken);
            return medicos
                .Where(m => !string.IsNullOrWhiteSpace(m.Cedula))
                .GroupBy(m => m.Cedula, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(m => m.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar médicos desde la API.");
            return [];
        }
    }

    private async Task<List<OperadorDto>> LoadOperadoresAsync(
        IReadOnlyList<EmpresaDto> empresas,
        CancellationToken cancellationToken)
    {
        var operadoresMap = new Dictionary<string, OperadorDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var empresa in empresas)
        {
            try
            {
                var operadores = await _apiClient.ObtenerOperadoresAsync(empresa.Codigo, cancellationToken);
                foreach (var operador in operadores)
                {
                    operadoresMap.TryAdd(operador.Cedula, operador);
                }
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No fue posible cargar operadores para empresa {Empresa}", empresa.Codigo);
            }
        }

        return operadoresMap.Values.OrderBy(o => o.Nombre, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<List<RadiologoRegistradoDto>> LoadRadiologosRegistradosAsync(
        IReadOnlyList<EmpresaDto> empresas,
        ServerConnectionInfo? connection,
        CancellationToken cancellationToken)
    {
        var catalogoEmpresas = empresas
            .Where(e => !string.IsNullOrWhiteSpace(e.Codigo))
            .ToDictionary(e => e.Codigo, e => e, StringComparer.OrdinalIgnoreCase);

        var radiologosMap = new Dictionary<string, RadiologoRegistradoDto>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var global = await _apiClient.ObtenerRadiologosRegistradosAsync(
                codigoEmpresa: null,
                connection: connection,
                cancellationToken: cancellationToken);

            foreach (var radiologo in EnriquecerRadiologosConCatalogo(global, catalogoEmpresas))
            {
                radiologosMap[BuildRadiologoKey(radiologo)] = radiologo;
            }
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogDebug(ex, "Carga global de radiólogos no disponible; se consulta por empresa.");
        }

        if (radiologosMap.Count == 0)
        {
            foreach (var empresa in empresas)
            {
                try
                {
                    var registros = await _apiClient.ObtenerRadiologosRegistradosAsync(
                        empresa.Codigo,
                        connection,
                        cancellationToken);

                    foreach (var radiologo in EnriquecerRadiologosConCatalogo(registros, catalogoEmpresas))
                    {
                        radiologosMap[BuildRadiologoKey(radiologo)] = radiologo;
                    }
                }
                catch (EsculapioApiException ex)
                {
                    _logger.LogWarning(ex, "No fue posible cargar radiólogos para empresa {Empresa}", empresa.Codigo);
                }
            }
        }

        return radiologosMap.Values
            .OrderBy(r => r.NombreMedico, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.NombreEmpresas, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildRadiologoKey(RadiologoRegistradoDto radiologo)
    {
        var empresa = radiologo.Empresas.FirstOrDefault() ?? string.Empty;
        return $"{empresa}|{radiologo.CedulaMedico}|{radiologo.CodDependencia}";
    }

    private static List<RadiologoRegistradoDto> EnriquecerRadiologosConCatalogo(
        IEnumerable<RadiologoRegistradoDto> registros,
        IReadOnlyDictionary<string, EmpresaDto> catalogoEmpresas)
    {
        return registros
            .Select(registro =>
            {
                var codigoEmpresa = registro.Empresas.FirstOrDefault() ?? string.Empty;
                var nombreEmpresas = registro.NombreEmpresas;

                if (string.IsNullOrWhiteSpace(nombreEmpresas)
                    && !string.IsNullOrWhiteSpace(codigoEmpresa)
                    && catalogoEmpresas.TryGetValue(codigoEmpresa, out var empresa))
                {
                    nombreEmpresas = empresa.Etiqueta;
                }
                else if (string.IsNullOrWhiteSpace(nombreEmpresas) && !string.IsNullOrWhiteSpace(codigoEmpresa))
                {
                    nombreEmpresas = EmpresaDto.FormatoEtiqueta(codigoEmpresa, string.Empty);
                }

                return registro with { NombreEmpresas = nombreEmpresas };
            })
            .Where(r => !string.IsNullOrWhiteSpace(r.CedulaMedico))
            .ToList();
    }

    private async Task<EstudiosViewModel> BuildEstudiosViewModelAsync(CancellationToken cancellationToken)
    {
        var model = new EstudiosViewModel();
        await PopulateEstudiosListsAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateEstudiosListsAsync(EstudiosViewModel model, CancellationToken cancellationToken)
    {
        var connection = GetConnectionInfo();
        model.EmpresasDisponibles = await LoadEmpresasAsync(cancellationToken);

        try
        {
            model.DependenciasDisponibles = (await _apiClient.ObtenerDependenciasAsync(connection, cancellationToken)).ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar dependencias desde la API.");
            model.DependenciasDisponibles = [];
        }

        model.EstudiosRegistrados = await LoadEstudiosRegistradosAsync(model.EmpresasDisponibles, connection, cancellationToken);
    }

    private async Task<List<ServicioDto>> LoadServiciosAsync(
        string codDependencia,
        CancellationToken cancellationToken)
    {
        try
        {
            var servicios = await _apiClient.ObtenerServiciosPorDependenciaAsync(codDependencia, cancellationToken);
            return servicios.ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar servicios para dependencia {CodDependencia}", codDependencia);
            return [];
        }
    }

    private async Task<List<EstudioEmpresaDto>> LoadEstudiosRegistradosAsync(
        IReadOnlyList<EmpresaDto> empresas,
        ServerConnectionInfo? connection,
        CancellationToken cancellationToken)
    {
        var catalogoEmpresas = empresas
            .Where(e => !string.IsNullOrWhiteSpace(e.Codigo))
            .ToDictionary(e => e.Codigo, e => e, StringComparer.OrdinalIgnoreCase);

        var estudiosMap = new Dictionary<string, EstudioEmpresaDto>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var global = await _apiClient.ObtenerEstudiosEmpresaAsync(
                codigoEmpresa: null,
                connection: connection,
                cancellationToken: cancellationToken);

            foreach (var estudio in EnriquecerEstudiosConCatalogo(global, catalogoEmpresas))
            {
                estudiosMap[BuildEstudioKey(estudio)] = estudio;
            }
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogDebug(ex, "Carga global de estudios no disponible; se consulta por empresa.");
        }

        if (estudiosMap.Count == 0)
        {
            foreach (var empresa in empresas)
            {
                try
                {
                    var registros = await _apiClient.ObtenerEstudiosEmpresaAsync(
                        empresa.Codigo,
                        connection,
                        cancellationToken);

                    foreach (var estudio in EnriquecerEstudiosConCatalogo(registros, catalogoEmpresas))
                    {
                        estudiosMap[BuildEstudioKey(estudio)] = estudio;
                    }
                }
                catch (EsculapioApiException ex)
                {
                    _logger.LogWarning(ex, "No fue posible cargar estudios para empresa {Empresa}", empresa.Codigo);
                }
            }
        }

        return OrdenarEstudiosRegistrados(estudiosMap.Values);
    }

    private static string BuildEstudioKey(EstudioEmpresaDto estudio) =>
        $"{estudio.Empresa}|{estudio.CodDependencia}";

    private static List<EstudioEmpresaDto> EnriquecerEstudiosConCatalogo(
        IEnumerable<EstudioEmpresaDto> registros,
        IReadOnlyDictionary<string, EmpresaDto> catalogoEmpresas)
    {
        return registros
            .Select(registro =>
            {
                if (catalogoEmpresas.TryGetValue(registro.Empresa, out var empresa))
                {
                    return registro with
                    {
                        NombreEmpresa = string.IsNullOrWhiteSpace(registro.NombreEmpresa)
                            ? empresa.Etiqueta
                            : registro.NombreEmpresa
                    };
                }

                return registro with
                {
                    NombreEmpresa = string.IsNullOrWhiteSpace(registro.NombreEmpresa)
                        ? registro.Empresa
                        : registro.NombreEmpresa
                };
            })
            .ToList();
    }

    private static List<EstudioEmpresaDto> OrdenarEstudiosRegistrados(IEnumerable<EstudioEmpresaDto> estudios) =>
        estudios
            .OrderBy(e => e.NombreEmpresa, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.NombreDependencia, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<List<OperadorRegistradoDto>> LoadOperadoresRegistradosAsync(
        IReadOnlyList<EmpresaDto> empresas,
        CancellationToken cancellationToken)
    {
        var operadoresMap = new Dictionary<string, (OperadorRegistradoDto Operador, HashSet<string> Empresas)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var empresa in empresas)
        {
            try
            {
                var operadores = await _apiClient.ObtenerOperadoresRegistradosAsync(empresa.Codigo, cancellationToken);
                foreach (var operador in operadores)
                {
                    if (!operadoresMap.TryGetValue(operador.CedulaOperador, out var entry))
                    {
                        entry = (operador, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        operadoresMap[operador.CedulaOperador] = entry;
                    }

                    foreach (var codigoEmpresa in operador.Empresas)
                    {
                        entry.Empresas.Add(codigoEmpresa);
                    }

                    entry.Empresas.Add(empresa.Codigo);
                }
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No fue posible cargar operadores registrados para empresa {Empresa}", empresa.Codigo);
            }
        }

        return operadoresMap.Values
            .Select(entry => entry.Operador with
            {
                Empresas = entry.Empresas.OrderBy(e => e, StringComparer.OrdinalIgnoreCase).ToList()
            })
            .OrderBy(o => o.NombreOperador, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ServerConnectionInfo? GetConnectionInfo()
    {
        var connectionJson = _sessionService.ObtenerConnectionString();
        if (string.IsNullOrEmpty(connectionJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ServerConnectionInfo>(connectionJson);
    }

    private void ApplyTempDataMessages()
    {
        if (TempData["SuccessMessage"] is string successMessage)
        {
            ViewData["SuccessMessage"] = successMessage;
        }

        if (TempData["ErrorMessage"] is string errorMessage)
        {
            ViewData["ErrorMessage"] = errorMessage;
        }
    }

    private static PlaceholderViewModel CreatePlaceholder(string titulo, string mensaje) =>
        new() { Titulo = titulo, Mensaje = mensaje };
}
