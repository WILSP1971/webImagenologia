using DocumentFormat.OpenXml.Office2016.Excel;
using System.Net.Http.Json;
using System.Text.Json;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Web.Services;

public class EsculapioApiClient : IEsculapioApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<EsculapioApiClient> _logger;

    public EsculapioApiClient(HttpClient httpClient, ILogger<EsculapioApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ServidorDto>> ObtenerServidoresAsync(CancellationToken cancellationToken = default)
    {
        const string url = "Usuarios/obtener-servidores";
        var servidores = await GetAsync<List<ServidorApiResponse>>(url, cancellationToken);
        return servidores?.Select(s => s.ToServidorDto()) ?? [];
    }

    public async Task<IEnumerable<EmpresaDto>> ObtenerEmpresasAsync(
        string ipConexion,
        string bdConexion,
        string portConexion,
        string usuario,
        string password,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["Ipconexion"] = ipConexion,
            ["BdConexion"] = bdConexion,
            ["PortConexion"] = portConexion,
            ["Usuario"] = usuario,
            ["PasswordUsu"] = password
        });

        var url = $"Usuarios/obtener-empresas{query}";
        var empresas = await GetAsync<List<EmpresaApiResponse>>(url, cancellationToken);
        return DedupeEmpresas(empresas?.Select(e => e.ToEmpresaDto()) ?? []);
    }

    public async Task<UsuarioConexionDto> ValidarConexionAsync(
        string ipConexion,
        string usuario,
        string password,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["IpConexion"] = ipConexion,
            ["Usuario"] = usuario,
            ["PasswordUsu"] = password
        });

        var url = $"Usuarios/obtener-validaconexion{query}";
        var body = await GetRawBodyAsync(url, cancellationToken);

        if (IsLoginErrorResponse(body))
        {
            throw new EsculapioApiException(System.Net.HttpStatusCode.Unauthorized, body);
        }

        List<UsuarioConexionApiResponse>? usuarios;
        try
        {
            usuarios = JsonSerializer.Deserialize<List<UsuarioConexionApiResponse>>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Respuesta inesperada de validación de conexión");
            throw new EsculapioApiException(System.Net.HttpStatusCode.Unauthorized, body);
        }

        if (usuarios is null || usuarios.Count == 0)
        {
            throw new EsculapioApiException(System.Net.HttpStatusCode.Unauthorized, body);
        }

        return usuarios[0].ToUsuarioConexionDto([]);
    }

    public async Task<DiagnosticoDto> ObtenerDiagnosticoCuentaAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["Empresa"] = empresa,
            ["NoCuenta"] = noCuenta.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        var url = $"Diagnosticos/obtener-diagnosticocuenta{query}";
        var diagnostico = await GetAsync<DiagnosticoDto>(url, cancellationToken);

        return diagnostico ?? throw new InvalidOperationException("La API no retornó datos de diagnóstico.");
    }

    public async Task<IEnumerable<NotaMedicaDto>> ObtenerNotasMedicasCuentaAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["Empresa"] = empresa,
            ["NoCuenta"] = noCuenta.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        var url = $"Diagnosticos/obtener-notasmedicascuenta{query}";
        var notas = await GetAsync<List<NotaMedicaDto>>(url, cancellationToken);
        return notas ?? [];
    }

    public async Task<IEnumerable<MedicoDto>> ObtenerMedicosAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var connectionQueryIpConexion = BuildConnectionQueryIpConexion(connection);
        var connectionQuery           = BuildConnectionQuery(connection);

        // Intentar primero con parámetros de conexión para obtener nombres completos
        var urls = new List<string>();

        if (!string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            var empresaP = BuildQueryString(
                new Dictionary<string, string?> { ["codigoEmp"] = codigoEmpresa });

            if (!string.IsNullOrEmpty(connectionQueryIpConexion))
            {
                urls.Add($"Imagenologia/obtener-radiologos{MergeQueries(empresaP, connectionQueryIpConexion)}");
            }

            if (!string.IsNullOrEmpty(connectionQuery))
            {
                urls.Add($"Imagenologia/obtener-radiologos{MergeQueries(empresaP, connectionQuery)}");
            }
        }

        if (!string.IsNullOrEmpty(connectionQueryIpConexion))
        {
            urls.Add($"Imagenologia/obtener-radiologos{connectionQueryIpConexion}");
        }

        if (!string.IsNullOrEmpty(connectionQuery))
        {
            urls.Add($"Imagenologia/obtener-radiologos{connectionQuery}");
        }

        // Sin parámetros solo como último recurso
        urls.Add("Imagenologia/obtener-radiologos");

        foreach (var url in urls.Distinct(StringComparer.Ordinal))
        {
            try
            {
                var body = await GetRawBodyAsync(url, cancellationToken);
                _logger.LogInformation("ObtenerMedicos raw [{Url}] → {Body}", url, body);
                var medicos = MapMedicosFromBody(body);

                if (medicos.Count > 0)
                {
                    _logger.LogDebug("Médicos cargados ({Count}) desde {Url}", medicos.Count, url);
                    return medicos;
                }
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar médicos desde {Url}", url);
            }
        }

        return [];
    }

    public async Task<IEnumerable<RadiologoRegistradoDto>> ObtenerRadiologosRegistradosAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var query in BuildImagenologiaEmpresaQueryVariants(codigoEmpresa, connection))
        {
            var url = $"Imagenologia/obtener-parametrosestudiosmedicos{query}";
            try
            {
                var body = await GetRawBodyAsync(url, cancellationToken);
                _logger.LogInformation("ObtenerRadiologos raw [{Url}] → {Body}", url, body);
                var radiologos = MapRadiologosRegistradosFromBody(body);

                if (radiologos.Count > 0)
                {
                    _logger.LogInformation("Radiólogos registrados ({Count}) desde {Url}", radiologos.Count, url);
                    return radiologos;
                }
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "Error al obtener radiólogos desde {Url}", url);
            }
        }

        return [];
    }

    private static List<MedicoDto> MapMedicosFromBody(string body)
    {
        var flexible = ApiFlexibleJson.ParseMedicos(body);
        if (flexible.Count > 0)
        {
            return flexible;
        }

        var mapped = TryDeserializeList<MedicoApiResponse>(body)?
            .Select(m => m.ToMedicoDto())
            .Where(m => !string.IsNullOrWhiteSpace(m.Cedula))
            .ToList() ?? [];

        return mapped;
    }

    private static List<RadiologoRegistradoDto> MapRadiologosRegistradosFromBody(string body)
    {
        var flexible = ApiFlexibleJson.ParseRadiologosRegistrados(body);
        if (flexible.Count > 0)
        {
            return flexible;
        }

        var mapped = TryDeserializeList<RadiologoRegistradoApiResponse>(body)?
            .Select(r => r.ToRadiologoRegistradoDto())
            .Where(r => !string.IsNullOrWhiteSpace(r.CedulaMedico))
            .ToList() ?? [];

        return mapped;
    }

    public Task RegistrarRadiologoAsync(
    RegistrarRadiologoRequest request,
    ServerConnectionInfo? connection = null,
    CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string?>();

        // Parámetros de conexión requeridos por la API
        if (connection is not null)
        {
            parameters["IpConexion"] = connection.IpConexion;
            parameters["BdConexion"] = connection.BdConexion;
            parameters["PortConexion"] = connection.PortConexion;
            parameters["Usuario"] = connection.Usuario;
            parameters["PasswordUsu"] = connection.Password;
        }

        // Parámetros del estudio (nombres exactos del endpoint [FromUri])
        parameters["codigoEmp"] = request.Empresa;
        parameters["Cedulamedico"] = request.CedulaMedico;
        parameters["UsuarioEsculapio"] = request.UsuarioEsculapio;
        parameters["CodDependencia"] = request.CodDependencia;
        parameters["Cantidad"] = request.Cantidad.ToString(System.Globalization.CultureInfo.InvariantCulture);
        parameters["Estado"] = request.Estado;
        parameters["Tipo"] = request.tipo;

        var query = BuildQueryString(parameters);
        // El endpoint de la API es [HttpGet], no POST
        return GetRawBodyAsync($"Imagenologia/registrar-estudiosmedicos{query}", cancellationToken);
    }


    public Task EliminarRadiologoAsync(
     RegistrarRadiologoRequest request,
    ServerConnectionInfo? connection = null,
    CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string?>();

        if (connection is not null)
        {
            parameters["IpConexion"] = connection.IpConexion;
            parameters["BdConexion"] = connection.BdConexion;
            parameters["PortConexion"] = connection.PortConexion;
            parameters["Usuario"] = connection.Usuario;
            parameters["PasswordUsu"] = connection.Password;
        }

        parameters["codigoEmp"] = request.Empresa;
        parameters["Cedulamedico"] = request.CedulaMedico;
        parameters["UsuarioEsculapio"] = request.UsuarioEsculapio;
        parameters["CodDependencia"] = request.CodDependencia;
        parameters["Cantidad"] = request.Cantidad.ToString(System.Globalization.CultureInfo.InvariantCulture);
        parameters["Estado"] = "I";
        parameters["Tipo"] = "I";

        var query = BuildQueryString(parameters);
        return GetRawBodyAsync($"Imagenologia/registrar-estudiosmedicos{query}", cancellationToken);
    }



    public async Task<IEnumerable<OperadorDto>> ObtenerOperadoresAsync(
        string empresa,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa
        });

        var url = $"Operadores/obtener-operadores{query}";
        var operadores = await GetAsync<List<OperadorApiResponse>>(url, cancellationToken);
        return operadores?.Select(o => o.ToOperadorDto()).Where(o => !string.IsNullOrWhiteSpace(o.Cedula)) ?? [];
    }

    public async Task<IEnumerable<OperadorRegistradoDto>> ObtenerOperadoresRegistradosAsync(
        string empresa,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa
        });

        var url = $"Operadores/obtener-registrados{query}";
        var operadores = await GetAsync<List<OperadorRegistradoApiResponse>>(url, cancellationToken);
        return operadores?.Select(o => o.ToOperadorRegistradoDto()).Where(o => !string.IsNullOrWhiteSpace(o.CedulaOperador)) ?? [];
    }

    public Task RegistrarOperadorAsync(
        RegistrarOperadorRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("Operadores/registrar", request, cancellationToken);

    public Task EliminarOperadorAsync(
        string cedula,
        CancellationToken cancellationToken = default) =>
        DeleteAsync($"Operadores/eliminar/{Uri.EscapeDataString(cedula)}", cancellationToken);

    public async Task<IEnumerable<DependenciaDto>> ObtenerDependenciasAsync(
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var urls = new List<string> { "Imagenologia/obtener-dependencias" };

        var connectionQuery = BuildConnectionQuery(connection);
        if (!string.IsNullOrEmpty(connectionQuery))
        {
            urls.Add($"Imagenologia/obtener-dependencias{connectionQuery}");
        }

        foreach (var url in urls)
        {
            var body = await GetRawBodyAsync(url, cancellationToken);
            var dependencias = MapDependenciasFromBody(body);

            if (dependencias.Count > 0)
            {
                return dependencias;
            }
        }

        return [];
    }

    private static List<DependenciaDto> MapDependenciasFromBody(string body)
    {
        var mapped = TryDeserializeList<DependenciaApiResponse>(body)?
            .Select(d => d.ToDependenciaDto())
            .Where(d => !string.IsNullOrWhiteSpace(d.CodDependencia))
            .ToList() ?? [];

        return mapped.Count > 0 ? mapped : ApiFlexibleJson.ParseDependencias(body);
    }

    public async Task<IEnumerable<ServicioDto>> ObtenerServiciosPorDependenciaAsync(
        string codDependencia,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["codDependencia"] = codDependencia
        });

        var url = $"Estudios/obtener-servicios{query}";
        var servicios = await GetAsync<List<ServicioDto>>(url, cancellationToken);
        return servicios ?? [];
    }

    public async Task<IEnumerable<EstudioEmpresaDto>> ObtenerEstudiosEmpresaAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var query in BuildImagenologiaEmpresaQueryVariants(codigoEmpresa, connection))
        {
            var url = $"Imagenologia/obtener-parametrosestudiosempresas{query}";
            var body = await GetRawBodyAsync(url, cancellationToken);
            var mapped = MapEstudiosEmpresaFromBody(body);

            if (mapped.Count > 0)
            {
                return mapped;
            }
        }

        return [];
    }

    private List<EstudioEmpresaDto> MapEstudiosEmpresaFromBody(string body)
    {
        var mapped = MapEstudiosEmpresaResponse(TryDeserializeList<EstudioEmpresaApiResponse>(body));
        return mapped.Count > 0 ? mapped : ApiFlexibleJson.ParseEstudiosEmpresa(body);
    }

    private static IEnumerable<string> BuildImagenologiaEmpresaQueryVariants(
        string? codigoEmpresa,
        ServerConnectionInfo? connection)
    {
        // Usar IpConexion (casing que funciona en los endpoints de registro)
        var connIpConexion = BuildConnectionQueryIpConexion(connection);
        var connIpconexion = BuildConnectionQuery(connection);       // variante alternativa

        var variants = new List<string>();

        if (!string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            foreach (var empresaKey in new[] { "CodigoEmpresas", "Empresa", "codigoEmp" })
            {
                var empresaQ = BuildQueryString(
                    new Dictionary<string, string?> { [empresaKey] = codigoEmpresa });

                if (!string.IsNullOrEmpty(connIpConexion))
                {
                    variants.Add(MergeQueries(empresaQ, connIpConexion));
                }

                if (!string.IsNullOrEmpty(connIpconexion))
                {
                    variants.Add(MergeQueries(empresaQ, connIpconexion));
                }
            }
        }

        // Sin empresa — conexión sola
        if (!string.IsNullOrEmpty(connIpConexion))
        {
            variants.Add(connIpConexion);
        }

        if (!string.IsNullOrEmpty(connIpconexion))
        {
            variants.Add(connIpconexion);
        }

        return variants.Distinct(StringComparer.Ordinal);
    }

    private static string BuildConnectionQueryIpConexion(ServerConnectionInfo? connection)
    {
        if (connection is null)
        {
            return string.Empty;
        }

        return BuildQueryString(new Dictionary<string, string?>
        {
            ["IpConexion"] = connection.IpConexion,
            ["BdConexion"] = connection.BdConexion,
            ["PortConexion"] = connection.PortConexion,
            ["Usuario"] = connection.Usuario,
            ["PasswordUsu"] = connection.Password
        });
    }

    private static string MergeQueries(string primary, string secondary)
    {
        if (string.IsNullOrEmpty(primary))
        {
            return secondary;
        }

        if (string.IsNullOrEmpty(secondary))
        {
            return primary;
        }

        return $"{primary}&{secondary.TrimStart('?')}";
    }

    private static string BuildConnectionQuery(ServerConnectionInfo? connection)
    {
        if (connection is null)
        {
            return string.Empty;
        }

        return BuildQueryString(new Dictionary<string, string?>
        {
            ["Ipconexion"] = connection.IpConexion,
            ["BdConexion"] = connection.BdConexion,
            ["PortConexion"] = connection.PortConexion,
            ["Usuario"] = connection.Usuario,
            ["PasswordUsu"] = connection.Password
        });
    }

    private static List<T>? TryDeserializeList<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(body, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private static List<EstudioEmpresaDto> MapEstudiosEmpresaResponse(
        List<EstudioEmpresaApiResponse>? estudios) =>
        estudios?
            .Select(e => e.ToEstudioEmpresaDto())
            .Where(e => !string.IsNullOrWhiteSpace(e.Empresa))
            .ToList() ?? [];

    public Task RegistrarEstudioEmpresaAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string?>();

        if (connection is not null)
        {
            parameters["IpConexion"]  = connection.IpConexion;
            parameters["BdConexion"]  = connection.BdConexion;
            parameters["PortConexion"] = connection.PortConexion;
            parameters["Usuario"]     = connection.Usuario;
            parameters["PasswordUsu"] = connection.Password;
        }

        parameters["codigoEmp"]      = request.Empresa;
        parameters["CodDependencia"] = request.codDependencia;
        parameters["Cantidad"]       = Convert.ToInt32(request.Cantidad)
                                           .ToString(System.Globalization.CultureInfo.InvariantCulture);
        parameters["Estado"]         = request.Estado;
        parameters["Tipo"]           = request.tipo;

        var query = BuildQueryString(parameters);
        _logger.LogInformation("RegistrarEstudio → Imagenologia/registrar-estudiosempresas{Query}", query);
        return GetRawBodyAsync($"Imagenologia/registrar-estudiosempresas{query}", cancellationToken);
    }

    public Task EliminarEstudioEmpresaAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string?>();

        if (connection is not null)
        {
            parameters["IpConexion"]  = connection.IpConexion;
            parameters["BdConexion"]  = connection.BdConexion;
            parameters["PortConexion"] = connection.PortConexion;
            parameters["Usuario"]     = connection.Usuario;
            parameters["PasswordUsu"] = connection.Password;
        }

        parameters["codigoEmp"]      = request.Empresa;
        parameters["CodDependencia"] = request.codDependencia;
        parameters["Cantidad"]       = Convert.ToInt32(request.Cantidad)
                                           .ToString(System.Globalization.CultureInfo.InvariantCulture);
        parameters["Estado"]         = "I";
        parameters["Tipo"]           = "U";

        var query = BuildQueryString(parameters);
        _logger.LogInformation("EliminarEstudio → Imagenologia/registrar-estudiosempresas{Query}", query);
        return GetRawBodyAsync($"Imagenologia/registrar-estudiosempresas{query}", cancellationToken);
    }

    private async Task InvokeEstudioEmpresaMutationAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection,
        CancellationToken cancellationToken)
    {
        if (connection is null)
        {
            throw new EsculapioApiException(
                System.Net.HttpStatusCode.BadRequest,
                "Se requieren datos de conexión al servidor para registrar/eliminar estudios.");
        }

        var businessParams = new Dictionary<string, string?>
        {
            ["codigoEmp"]      = request.Empresa,
            ["CodDependencia"] = request.codDependencia,
            ["Cantidad"]       = Convert.ToInt32(request.Cantidad)
                                     .ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Estado"]         = request.Estado,
            ["Tipo"]           = request.tipo
        };

        _logger.LogInformation(
            "InvokeEstudioEmpresa → Empresa={E} Dep={D} Cant={C} Estado={S} Tipo={T}",
            request.Empresa, request.codDependencia, request.Cantidad, request.Estado, request.tipo);

        // Probar dos variantes de nombre del parámetro IP (IpConexion vs Ipconexion)
        var queryVariants = new List<string>
        {
            BuildQueryString(MergeParams(businessParams, BuildConnectionParamsIpConexion(connection))),
            BuildQueryString(MergeParams(businessParams, BuildConnectionParamsIpconexion(connection)))
        }.Distinct(StringComparer.Ordinal).ToList();

        EsculapioApiException? lastError = null;

        foreach (var query in queryVariants)
        {
            var fullUrl = $"Imagenologia/registrar-estudiosempresas{query}";
            try
            {
                _logger.LogInformation("Llamando API → {Url}", fullUrl);
                var body = await GetRawBodyAsync(fullUrl, cancellationToken);

                _logger.LogInformation("Respuesta API (raw) → [{Body}]", body);
                EnsureMutationSuccess(body);
                _logger.LogInformation("Estudio registrado OK — empresa={E} dep={D}", request.Empresa, request.codDependencia);
                return;
            }
            catch (EsculapioApiException ex)
            {
                lastError = ex;
                _logger.LogWarning("Fallo mutación estudio → {Url} | {Msg}", fullUrl, ex.Message);
            }
        }

        throw lastError ?? new EsculapioApiException(
            System.Net.HttpStatusCode.BadRequest,
            "No fue posible registrar el estudio en la API.");
    }

    private static Dictionary<string, string?> MergeParams(
        Dictionary<string, string?> business,
        Dictionary<string, string?> connection) =>
        business.Concat(connection).ToDictionary(kv => kv.Key, kv => kv.Value);

    private static Dictionary<string, string?> BuildConnectionParamsIpConexion(ServerConnectionInfo? connection)
    {
        if (connection is null)
        {
            return [];
        }

        return new Dictionary<string, string?>
        {
            ["IpConexion"] = connection.IpConexion,
            ["BdConexion"] = connection.BdConexion,
            ["PortConexion"] = connection.PortConexion,
            ["Usuario"] = connection.Usuario,
            ["PasswordUsu"] = connection.Password
        };
    }

    private static Dictionary<string, string?> BuildConnectionParamsIpconexion(ServerConnectionInfo? connection)
    {
        if (connection is null)
        {
            return [];
        }

        return new Dictionary<string, string?>
        {
            ["Ipconexion"] = connection.IpConexion,
            ["BdConexion"] = connection.BdConexion,
            ["PortConexion"] = connection.PortConexion,
            ["Usuario"] = connection.Usuario,
            ["PasswordUsu"] = connection.Password
        };
    }

    private static void EnsureMutationSuccess(string body)
    {
        // La API devuelve Ok("Ok") → body = "Ok" con comillas JSON.
        // Cualquier otra respuesta (vacía, array, texto de error) se trata como fallo.
        var trimmed = body.Trim().Trim('"');

        if (trimmed.Equals("Ok", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var detail = string.IsNullOrWhiteSpace(trimmed) ? "La API no devolvió confirmación." : trimmed;
        throw new EsculapioApiException(System.Net.HttpStatusCode.BadRequest, detail);
    }

    public async Task<IEnumerable<AsignacionMedicoDto>> ObtenerAsignacionesMedicoAsync(
        string empresa,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa
        });

        var url = $"Asignacion/obtener-registrados{query}";
        var asignaciones = await GetAsync<List<AsignacionMedicoDto>>(url, cancellationToken);
        return asignaciones ?? [];
    }

    public Task RegistrarAsignacionMedicoAsync(
        RegistrarAsignacionMedicoRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("Asignacion/registrar", request, cancellationToken);

    public Task EliminarAsignacionMedicoAsync(
        string empresa,
        string cedulaMedico,
        string codDependencia,
        string codServicio,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["cedulaMedico"] = cedulaMedico,
            ["codDependencia"] = codDependencia,
            ["codServicio"] = codServicio
        });

        return DeleteAsync($"Asignacion/eliminar{query}", cancellationToken);
    }

    public async Task<IEnumerable<AutomatizacionDto>> ObtenerAutomatizacionesAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "Automatizacion/obtener-registrados";
        var automatizaciones = await GetAsync<List<AutomatizacionDto>>(url, cancellationToken);
        return automatizaciones ?? [];
    }

    public Task RegistrarAutomatizacionAsync(
        RegistrarAutomatizacionRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("Automatizacion/registrar", request, cancellationToken);

    public Task ToggleAutomatizacionEstadoAsync(
        ToggleAutomatizacionEstadoRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("Automatizacion/toggle-estado", request, cancellationToken);

    public async Task<IEnumerable<EstudioProgramadoDto>> ObtenerEstudiosProgramadosAsync(
        string empresa,
        string cedulaMedico,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["cedulaMedico"] = cedulaMedico,
            ["fecha"] = fecha.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        });

        var url = $"Programacion/obtener-programados{query}";
        var estudios = await GetAsync<List<EstudioProgramadoDto>>(url, cancellationToken);
        return estudios ?? [];
    }

    public async Task<IEnumerable<LecturaDto>> ObtenerLecturasAsync(
        string empresa,
        DateOnly fechaInicial,
        DateOnly fechaFinal,
        string? cedulaMedico,
        string? estado,
        long? consecutivo = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["fechaInicial"] = fechaInicial.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["fechaFinal"] = fechaFinal.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["cedulaMedico"] = cedulaMedico,
            ["estado"] = estado,
            ["consecutivo"] = consecutivo?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        var url = $"Programacion/obtener-lecturas{query}";
        var lecturas = await GetAsync<List<LecturaDto>>(url, cancellationToken);
        return lecturas ?? [];
    }

    public async Task SubirAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        Stream audioStream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(empresa), "empresa");
        content.Add(new StringContent(consecutivo.ToString(System.Globalization.CultureInfo.InvariantCulture)), "consecutivo");

        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "archivo", fileName);

        await PostMultipartAsync("Programacion/subir-audio", content, cancellationToken);
    }

    public async Task<(byte[] Content, string ContentType)> ObtenerAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["consecutivo"] = consecutivo.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        var url = $"Programacion/obtener-audio{query}";

        try
        {
            _logger.LogDebug("GET {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, url);
                throw new EsculapioApiException(response.StatusCode, body);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";
            return (bytes, mediaType);
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", url);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    public Task EliminarAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["consecutivo"] = consecutivo.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        return DeleteAsync($"Programacion/eliminar-audio{query}", cancellationToken);
    }

    public async Task<IEnumerable<EstudioSinResultadoDto>> ObtenerEstudiosSinResultadoAsync(
        string empresa,
        DateOnly fechaInicial,
        DateOnly fechaFinal,
        string? codDependencia,
        string? codServicio,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["fechaInicial"] = fechaInicial.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["fechaFinal"] = fechaFinal.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["codDependencia"] = codDependencia,
            ["codServicio"] = codServicio
        });

        var url = $"Reportes/obtener-sin-resultado{query}";
        var estudios = await GetAsync<List<EstudioSinResultadoDto>>(url, cancellationToken);
        return estudios ?? [];
    }

    private async Task PostMultipartAsync(
        string relativeUrl,
        HttpContent content,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("POST multipart {Url}", relativeUrl);
            var response = await _httpClient.PostAsync(relativeUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, relativeUrl);
                throw new EsculapioApiException(response.StatusCode, body);
            }
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", relativeUrl);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    private static bool IsLoginErrorResponse(string body)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                var error = JsonSerializer.Deserialize<LoginErrorApiResponse>(trimmed, JsonOptions);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    return error.Message.Contains("Error", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (JsonException)
            {
                // continuar con otras comprobaciones
            }
        }

        if (trimmed.StartsWith("\"", StringComparison.Ordinal))
        {
            try
            {
                var message = JsonSerializer.Deserialize<string>(trimmed, JsonOptions);
                return message?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (JsonException)
            {
                return trimmed.Contains("Error", StringComparison.OrdinalIgnoreCase);
            }
        }

        return trimmed.Contains("Error", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GetRawBodyAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("GET {Url}", relativeUrl);
            var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, relativeUrl);
                throw new EsculapioApiException(response.StatusCode, body);
            }

            return body;
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", relativeUrl);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("GET {Url}", relativeUrl);
            var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, relativeUrl);
                throw new EsculapioApiException(response.StatusCode, body);
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", relativeUrl);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    private async Task PostAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("POST {Url}", relativeUrl);
            // Todos los parámetros van en el query string; el cuerpo es intencionalmente vacío.
            var response = await _httpClient.PostAsync(relativeUrl, content: null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, relativeUrl);
                throw new EsculapioApiException(response.StatusCode, body);
            }
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", relativeUrl);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    private async Task PostAsync<TRequest>(
        string relativeUrl,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("POST {Url}", relativeUrl);
            var response = await _httpClient.PostAsJsonAsync(relativeUrl, payload, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, relativeUrl);
                throw new EsculapioApiException(response.StatusCode, body);
            }
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", relativeUrl);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    private async Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("DELETE {Url}", relativeUrl);
            var response = await _httpClient.DeleteAsync(relativeUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error API {StatusCode} en {Url}", response.StatusCode, relativeUrl);
                throw new EsculapioApiException(response.StatusCode, body);
            }
        }
        catch (EsculapioApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error de comunicación con la API en {Url}", relativeUrl);
            throw new EsculapioApiException(System.Net.HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string?> parameters)
    {
        var pairs = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");

        var query = string.Join('&', pairs);
        return string.IsNullOrEmpty(query) ? string.Empty : $"?{query}";
    }

    private static IEnumerable<EmpresaDto> DedupeEmpresas(IEnumerable<EmpresaDto> empresas) =>
        empresas
            .Where(e => !string.IsNullOrWhiteSpace(e.Codigo))
            .GroupBy(e => e.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(e => e.Nombre, StringComparer.OrdinalIgnoreCase);
}
