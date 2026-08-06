using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Tests;

public class LoginTests : IClassFixture<LoginWebApplicationFactory>
{
    private readonly LoginWebApplicationFactory _factory;

    public LoginTests(LoginWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginPage = await client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);
        var serverKey = ExtractServerOptionValue(html);

        var form = new Dictionary<string, string>
        {
            ["Usuario"] = "admin",
            ["Password"] = "secret",
            ["ServidorSeleccionado"] = serverKey,
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectLocation = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.True(
            redirectLocation.Contains("/Home/Index", StringComparison.OrdinalIgnoreCase)
            || redirectLocation == "/"
            || redirectLocation.EndsWith("/Home", StringComparison.OrdinalIgnoreCase),
            $"Redirect inesperado: {redirectLocation}");

        var homeResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = false;

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginPage = await client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);
        var serverKey = ExtractServerOptionValue(html);

        var form = new Dictionary<string, string>
        {
            ["Usuario"] = "admin",
            ["Password"] = "wrong",
            ["ServidorSeleccionado"] = serverKey,
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("incorrectos", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_ClearsSessionAndRedirectsToLogin()
    {
        _factory.Reset();
        _factory.ApiClient.ValidarSucceeds = true;

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginPage = await client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);
        var serverKey = ExtractServerOptionValue(html);

        var form = new Dictionary<string, string>
        {
            ["Usuario"] = "admin",
            ["Password"] = "secret",
            ["ServidorSeleccionado"] = serverKey,
            ["__RequestVerificationToken"] = token
        };

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var logoutResponse = await client.GetAsync("/Account/Logout");
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Contains("/Account/Login", logoutResponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        var homeResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, homeResponse.StatusCode);
        Assert.Contains("/Account/Login", homeResponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SessionService_StoresEncryptedConnectionString()
    {
        _factory.Reset();
        using var scope = _factory.Services.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = httpContext;

        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        const string plainConnection = """{"IpConexion":"10.0.0.1","BdConexion":"db","PortConexion":"3306"}""";

        sessionService.GuardarConnectionString(plainConnection);

        var storedEncrypted = httpContext.Session.GetString("EncryptedConnectionString");
        Assert.False(string.IsNullOrEmpty(storedEncrypted));
        Assert.DoesNotContain("10.0.0.1", storedEncrypted, StringComparison.Ordinal);

        var recovered = sessionService.ObtenerConnectionString();
        Assert.Equal(plainConnection, recovered);
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\"";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "No se encontró el token antiforgery.");

        var valueIndex = html.IndexOf("value=\"", markerIndex, StringComparison.Ordinal) + "value=\"".Length;
        var endIndex = html.IndexOf('"', valueIndex);
        return html[valueIndex..endIndex];
    }

    private static string ExtractServerOptionValue(string html)
    {
        const string marker = "Servidor QA";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "No se encontró la opción de servidor en el HTML.");

        var valueIndex = html.LastIndexOf("value=\"", markerIndex, StringComparison.Ordinal) + "value=\"".Length;
        var endIndex = html.IndexOf('"', valueIndex);
        return html[valueIndex..endIndex];
    }
}

public class LoginWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeEsculapioApiClient ApiClient { get; } = new();

    public FakeN8nWebhookClient N8nWebhookClient { get; } = new();

    public void Reset()
    {
        ApiClient.Reset();
        N8nWebhookClient.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        Program.ConfigureTestServices = services =>
        {
            services.AddSingleton<IEsculapioApiClient>(ApiClient);
            services.AddSingleton<IN8nWebhookClient>(N8nWebhookClient);
        };
    }

    protected override void Dispose(bool disposing)
    {
        Program.ConfigureTestServices = null;
        base.Dispose(disposing);
    }
}

public sealed class FakeEsculapioApiClient : IEsculapioApiClient
{
    public bool ValidarSucceeds { get; set; } = true;

    public UsuarioConexionDto UsuarioValido { get; set; } = new(
        "admin.test",
        "Administrador Demo",
        RoleNames.Administrador,
        [new EmpresaDto("01", "Empresa Demo")]);

    public List<ServidorDto> Servidores { get; set; } =
    [
        new("Servidor QA", "10.0.0.5", "esculapio_qa", "3306")
    ];

    public List<MedicoDto> Medicos { get; set; } =
    [
        new("1234567890", "Dr. Juan Pérez"),
        new("0987654321", "Dra. Ana López")
    ];

    public List<RadiologoRegistradoDto> RadiologosRegistrados { get; set; } = [];

    public List<OperadorDto> Operadores { get; set; } =
    [
        new("1122334455", "María González"),
        new("5544332211", "Carlos Ramírez")
    ];

    public List<OperadorRegistradoDto> OperadoresRegistrados { get; set; } = [];

    public List<DependenciaDto> Dependencias { get; set; } =
    [
        new("DEP01", "Imagenología"),
        new("DEP02", "Laboratorio")
    ];

    public Dictionary<string, List<ServicioDto>> ServiciosPorDependencia { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DEP01"] =
        [
            new("SRV01", "Radiografía", "DEP01", "ESQ01"),
            new("SRV02", "Tomografía", "DEP01", "ESQ02")
        ],
        ["DEP02"] =
        [
            new("SRV03", "Hemograma", "DEP02", "ESQ03")
        ]
    };

    public List<EstudioEmpresaDto> EstudiosRegistrados { get; set; } = [];

    public List<AsignacionMedicoDto> AsignacionesRegistradas { get; set; } = [];

    public List<AutomatizacionDto> AutomatizacionesRegistradas { get; set; } = [];

    public List<EstudioProgramadoDto> EstudiosProgramados { get; set; } = [];

    public List<LecturaDto> Lecturas { get; set; } = [];

    public List<EstudioSinResultadoDto> EstudiosSinResultado { get; set; } = [];

    public Dictionary<string, (byte[] Content, string ContentType)> AudioStorage { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    private int _nextAutomatizacionId = 1;

    public void Reset()
    {
        ValidarSucceeds = true;
        UsuarioValido = new UsuarioConexionDto(
            "admin.test",
            "Administrador Demo",
            RoleNames.Administrador,
            [new EmpresaDto("01", "Empresa Demo")]);
        Servidores =
        [
            new ServidorDto("Servidor QA", "10.0.0.5", "esculapio_qa", "3306")
        ];
        Medicos =
        [
            new MedicoDto("1234567890", "Dr. Juan Pérez"),
            new MedicoDto("0987654321", "Dra. Ana López")
        ];
        RadiologosRegistrados = [];
        Operadores =
        [
            new OperadorDto("1122334455", "María González"),
            new OperadorDto("5544332211", "Carlos Ramírez")
        ];
        OperadoresRegistrados = [];
        Dependencias =
        [
            new DependenciaDto("DEP01", "Imagenología"),
            new DependenciaDto("DEP02", "Laboratorio")
        ];
        ServiciosPorDependencia = new Dictionary<string, List<ServicioDto>>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEP01"] =
            [
                new ServicioDto("SRV01", "Radiografía", "DEP01", "ESQ01"),
                new ServicioDto("SRV02", "Tomografía", "DEP01", "ESQ02")
            ],
            ["DEP02"] =
            [
                new ServicioDto("SRV03", "Hemograma", "DEP02", "ESQ03")
            ]
        };
        EstudiosRegistrados = [];
        AsignacionesRegistradas = [];
        AutomatizacionesRegistradas = [];
        EstudiosProgramados = [];
        Lecturas = [];
        EstudiosSinResultado = [];
        AudioStorage.Clear();
        _nextAutomatizacionId = 1;
    }

    public Task<IEnumerable<ServidorDto>> ObtenerServidoresAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<ServidorDto>>(Servidores);

    public Task<IEnumerable<EmpresaDto>> ObtenerEmpresasAsync(
        string ipConexion,
        string bdConexion,
        string portConexion,
        string usuario,
        string password,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<EmpresaDto>>(UsuarioValido.EmpresasAsignadas);

    public Task<IEnumerable<MedicoDto>> ObtenerMedicosAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<MedicoDto>>(Medicos);

    public Task<IEnumerable<RadiologoRegistradoDto>> ObtenerRadiologosRegistradosAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<RadiologoRegistradoDto> radiologos = RadiologosRegistrados;

        if (!string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            radiologos = radiologos.Where(r =>
                r.Empresas.Any(e => e.Equals(codigoEmpresa, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.FromResult<IEnumerable<RadiologoRegistradoDto>>(radiologos.ToList());
    }

    public Task RegistrarRadiologoAsync(
        RegistrarRadiologoRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var medico = Medicos.FirstOrDefault(m =>
            m.Cedula.Equals(request.CedulaMedico, StringComparison.OrdinalIgnoreCase));

        var index = RadiologosRegistrados.FindIndex(r =>
            r.CedulaMedico.Equals(request.CedulaMedico, StringComparison.OrdinalIgnoreCase)
            && r.CodDependencia.Equals(request.CodDependencia, StringComparison.OrdinalIgnoreCase)
            && r.Empresas.Any(e => e.Equals(request.Empresa, StringComparison.OrdinalIgnoreCase)));

        var radiologo = new RadiologoRegistradoDto(
            request.CedulaMedico,
            medico?.Nombre ?? request.CedulaMedico,
            request.UsuarioEsculapio,
            request.CodDependencia,
            string.Empty,
            request.Cantidad,
            [request.Empresa],
            EmpresaDto.FormatoEtiqueta(request.Empresa, request.Empresa));

        if (index >= 0)
        {
            RadiologosRegistrados[index] = radiologo;
        }
        else
        {
            RadiologosRegistrados.Add(radiologo);
        }

        return Task.CompletedTask;
    }

    public Task EliminarRadiologoAsync(
        RegistrarRadiologoRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var index = RadiologosRegistrados.FindIndex(r =>
            r.CedulaMedico.Equals(request.CedulaMedico, StringComparison.OrdinalIgnoreCase)
            && r.CodDependencia.Equals(request.CodDependencia, StringComparison.OrdinalIgnoreCase)
            && r.Empresas.Any(e => e.Equals(request.Empresa, StringComparison.OrdinalIgnoreCase)));

        if (index >= 0)
        {
            RadiologosRegistrados.RemoveAt(index);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<OperadorDto>> ObtenerOperadoresAsync(
        string empresa,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<OperadorDto>>(Operadores);

    public Task<IEnumerable<OperadorRegistradoDto>> ObtenerOperadoresRegistradosAsync(
        string empresa,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<OperadorRegistradoDto>>(OperadoresRegistrados);

    public Task RegistrarOperadorAsync(
        RegistrarOperadorRequest request,
        CancellationToken cancellationToken = default)
    {
        OperadoresRegistrados.Add(new OperadorRegistradoDto(
            request.CedulaOperador,
            request.NombreOperador,
            request.UsuarioEsculapio,
            request.Empresas.ToList()));
        return Task.CompletedTask;
    }

    public Task EliminarOperadorAsync(
        string cedula,
        CancellationToken cancellationToken = default)
    {
        var index = OperadoresRegistrados.FindIndex(o =>
            o.CedulaOperador.Equals(cedula, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            OperadoresRegistrados.RemoveAt(index);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<DependenciaDto>> ObtenerDependenciasAsync(
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<DependenciaDto>>(Dependencias);

    public Task<IEnumerable<ServicioDto>> ObtenerServiciosPorDependenciaAsync(
        string codDependencia,
        CancellationToken cancellationToken = default)
    {
        if (ServiciosPorDependencia.TryGetValue(codDependencia, out var servicios))
        {
            return Task.FromResult<IEnumerable<ServicioDto>>(servicios);
        }

        return Task.FromResult<IEnumerable<ServicioDto>>(Array.Empty<ServicioDto>());
    }

    public Task<IEnumerable<EstudioEmpresaDto>> ObtenerEstudiosEmpresaAsync(
        string? codigoEmpresa = null,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<EstudioEmpresaDto> estudios = EstudiosRegistrados;

        if (!string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            estudios = estudios.Where(e =>
                e.Empresa.Equals(codigoEmpresa, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IEnumerable<EstudioEmpresaDto>>(estudios.ToList());
    }

    public Task RegistrarEstudioEmpresaAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var dependencia = Dependencias.FirstOrDefault(d =>
            d.CodDependencia.Equals(request.codDependencia, StringComparison.OrdinalIgnoreCase));

        var index = EstudiosRegistrados.FindIndex(e =>
            e.Empresa.Equals(request.Empresa, StringComparison.OrdinalIgnoreCase)
            && e.CodDependencia.Equals(request.codDependencia, StringComparison.OrdinalIgnoreCase));

        var estudio = new EstudioEmpresaDto(
            request.Empresa,
            request.codDependencia,
            request.Cantidad,
            request.Estado,
            dependencia?.NombreDependencia ?? request.codDependencia,
            EmpresaDto.FormatoEtiqueta(request.Empresa, request.Empresa));

        if (index >= 0)
        {
            EstudiosRegistrados[index] = estudio;
        }
        else
        {
            EstudiosRegistrados.Add(estudio);
        }

        return Task.CompletedTask;
    }

    public Task EliminarEstudioEmpresaAsync(
        RegistrarEstudioEmpresaRequest request,
        ServerConnectionInfo? connection = null,
        CancellationToken cancellationToken = default)
    {
        var index = EstudiosRegistrados.FindIndex(e =>
            e.Empresa.Equals(request.Empresa, StringComparison.OrdinalIgnoreCase)
            && e.CodDependencia.Equals(request.codDependencia, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            EstudiosRegistrados.RemoveAt(index);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<AsignacionMedicoDto>> ObtenerAsignacionesMedicoAsync(
        string empresa,
        CancellationToken cancellationToken = default)
    {
        var asignaciones = AsignacionesRegistradas
            .Where(a => a.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IEnumerable<AsignacionMedicoDto>>(asignaciones);
    }

    public Task RegistrarAsignacionMedicoAsync(
        RegistrarAsignacionMedicoRequest request,
        CancellationToken cancellationToken = default)
    {
        var dependencia = Dependencias.FirstOrDefault(d =>
            d.CodDependencia.Equals(request.CodDependencia, StringComparison.OrdinalIgnoreCase));
        var servicios = ServiciosPorDependencia.GetValueOrDefault(request.CodDependencia) ?? [];
        var servicio = servicios.FirstOrDefault(s =>
            s.CodServicio.Equals(request.CodServicio, StringComparison.OrdinalIgnoreCase));
        var medico = Medicos.FirstOrDefault(m =>
            m.Cedula.Equals(request.CedulaMedico, StringComparison.OrdinalIgnoreCase));

        var index = AsignacionesRegistradas.FindIndex(a =>
            a.Empresa.Equals(request.Empresa, StringComparison.OrdinalIgnoreCase)
            && a.CedulaMedico.Equals(request.CedulaMedico, StringComparison.OrdinalIgnoreCase)
            && a.CodDependencia.Equals(request.CodDependencia, StringComparison.OrdinalIgnoreCase)
            && a.CodServicio.Equals(request.CodServicio, StringComparison.OrdinalIgnoreCase));

        var asignacion = new AsignacionMedicoDto(
            request.Empresa,
            request.CedulaMedico,
            medico?.Nombre ?? request.CedulaMedico,
            request.CodDependencia,
            request.CodServicio,
            request.Cantidad,
            request.Estado,
            dependencia?.NombreDependencia ?? request.CodDependencia,
            servicio?.NombreServicio ?? request.CodServicio);

        if (index >= 0)
        {
            AsignacionesRegistradas[index] = asignacion;
        }
        else
        {
            AsignacionesRegistradas.Add(asignacion);
        }

        return Task.CompletedTask;
    }

    public Task EliminarAsignacionMedicoAsync(
        string empresa,
        string cedulaMedico,
        string codDependencia,
        string codServicio,
        CancellationToken cancellationToken = default)
    {
        var index = AsignacionesRegistradas.FindIndex(a =>
            a.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
            && a.CedulaMedico.Equals(cedulaMedico, StringComparison.OrdinalIgnoreCase)
            && a.CodDependencia.Equals(codDependencia, StringComparison.OrdinalIgnoreCase)
            && a.CodServicio.Equals(codServicio, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            AsignacionesRegistradas.RemoveAt(index);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<AutomatizacionDto>> ObtenerAutomatizacionesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<AutomatizacionDto>>(AutomatizacionesRegistradas);

    public Task RegistrarAutomatizacionAsync(
        RegistrarAutomatizacionRequest request,
        CancellationToken cancellationToken = default)
    {
        var index = AutomatizacionesRegistradas.FindIndex(a =>
            a.TipoProgramacion.Equals(request.TipoProgramacion, StringComparison.OrdinalIgnoreCase));

        var automatizacion = new AutomatizacionDto(
            request.IdAutomatizacion ?? (index >= 0
                ? AutomatizacionesRegistradas[index].IdAutomatizacion
                : _nextAutomatizacionId++),
            request.TipoProgramacion,
            request.Frecuencia,
            request.HoraAutomatizacion,
            request.Estado);

        if (index >= 0)
        {
            AutomatizacionesRegistradas[index] = automatizacion;
        }
        else
        {
            AutomatizacionesRegistradas.Add(automatizacion);
        }

        return Task.CompletedTask;
    }

    public Task ToggleAutomatizacionEstadoAsync(
        ToggleAutomatizacionEstadoRequest request,
        CancellationToken cancellationToken = default)
    {
        var index = AutomatizacionesRegistradas.FindIndex(a => a.IdAutomatizacion == request.IdAutomatizacion);

        if (index >= 0)
        {
            var current = AutomatizacionesRegistradas[index];
            AutomatizacionesRegistradas[index] = current with { Estado = request.Estado };
        }

        return Task.CompletedTask;
    }

    public Task<UsuarioConexionDto> ValidarConexionAsync(
        string ipConexion,
        string usuario,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!ValidarSucceeds)
        {
            throw new EsculapioApiException(HttpStatusCode.Unauthorized, null);
        }

        return Task.FromResult(UsuarioValido);
    }

    public Task<DiagnosticoDto> ObtenerDiagnosticoCuentaAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new DiagnosticoDto(empresa, noCuenta, "Diagnóstico de prueba"));

    public Task<IEnumerable<NotaMedicaDto>> ObtenerNotasMedicasCuentaAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<NotaMedicaDto>>(
        [
            new NotaMedicaDto(empresa, noCuenta, "Nota médica de prueba", DateTime.Today)
        ]);

    public Task<IEnumerable<EstudioProgramadoDto>> ObtenerEstudiosProgramadosAsync(
        string empresa,
        string cedulaMedico,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        var estudios = EstudiosProgramados
            .Where(e => e.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
                && e.CedulaMedico.Equals(cedulaMedico, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IEnumerable<EstudioProgramadoDto>>(estudios);
    }

    public Task<IEnumerable<LecturaDto>> ObtenerLecturasAsync(
        string empresa,
        DateOnly fechaInicial,
        DateOnly fechaFinal,
        string? cedulaMedico,
        string? estado,
        long? consecutivo = null,
        CancellationToken cancellationToken = default)
    {
        var query = Lecturas.Where(l => l.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase));

        if (consecutivo.HasValue)
        {
            query = query.Where(l => l.Consecutivo == consecutivo.Value);
        }
        else
        {
            query = query.Where(l =>
                l.FechaProgramacion >= fechaInicial && l.FechaProgramacion <= fechaFinal);

            if (!string.IsNullOrWhiteSpace(cedulaMedico))
            {
                query = query.Where(l =>
                    l.CedulaMedico.Equals(cedulaMedico, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(l =>
                    l.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)
                    || (estado.Equals("PEN", StringComparison.OrdinalIgnoreCase)
                        && l.Estado.Equals("PEND", StringComparison.OrdinalIgnoreCase))
                    || (estado.Equals("LEI", StringComparison.OrdinalIgnoreCase)
                        && l.Estado.Equals("LEIDO", StringComparison.OrdinalIgnoreCase)));
            }
        }

        return Task.FromResult<IEnumerable<LecturaDto>>(query.ToList());
    }

    public Task<IEnumerable<EstudioSinResultadoDto>> ObtenerEstudiosSinResultadoAsync(
        string empresa,
        DateOnly fechaInicial,
        DateOnly fechaFinal,
        string? codDependencia,
        string? codServicio,
        CancellationToken cancellationToken = default)
    {
        var query = EstudiosSinResultado
            .Where(e => e.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
                && e.FechaOrden >= fechaInicial
                && e.FechaOrden <= fechaFinal);

        if (!string.IsNullOrWhiteSpace(codDependencia))
        {
            query = query.Where(e =>
                e.Dependencia.Contains(codDependencia, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(codServicio))
        {
            query = query.Where(e =>
                e.Servicio.Contains(codServicio, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IEnumerable<EstudioSinResultadoDto>>(query.ToList());
    }

    public Task SubirAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        Stream audioStream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        audioStream.CopyTo(memory);
        AudioStorage[BuildAudioKey(empresa, consecutivo)] = (memory.ToArray(), contentType);

        var index = EstudiosProgramados.FindIndex(e =>
            e.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
            && e.Consecutivo == consecutivo);

        if (index >= 0)
        {
            var current = EstudiosProgramados[index];
            EstudiosProgramados[index] = current with { TieneAudio = true };
        }

        return Task.CompletedTask;
    }

    public Task<(byte[] Content, string ContentType)> ObtenerAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        CancellationToken cancellationToken = default)
    {
        if (AudioStorage.TryGetValue(BuildAudioKey(empresa, consecutivo), out var audio))
        {
            return Task.FromResult(audio);
        }

        throw new EsculapioApiException(HttpStatusCode.NotFound, null);
    }

    public Task EliminarAudioProgramacionAsync(
        string empresa,
        long consecutivo,
        CancellationToken cancellationToken = default)
    {
        AudioStorage.Remove(BuildAudioKey(empresa, consecutivo));

        var index = EstudiosProgramados.FindIndex(e =>
            e.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
            && e.Consecutivo == consecutivo);

        if (index >= 0)
        {
            var current = EstudiosProgramados[index];
            EstudiosProgramados[index] = current with { TieneAudio = false };
        }

        return Task.CompletedTask;
    }

    private static string BuildAudioKey(string empresa, long consecutivo) =>
        $"{empresa}:{consecutivo}";
}

public sealed class MockApiMessageHandler : HttpMessageHandler
{
    private readonly IReadOnlyDictionary<string, (HttpStatusCode StatusCode, string Body)> _responses;

    public MockApiMessageHandler(IReadOnlyDictionary<string, (HttpStatusCode StatusCode, string Body)> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        var match = _responses
            .Where(entry => path.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.Key.Length)
            .FirstOrDefault();

        if (match.Key is null)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var response = new HttpResponseMessage(match.Value.StatusCode)
        {
            Content = new StringContent(match.Value.Body, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public IEnumerable<string> Keys => _store.Keys;

    public string Id { get; } = Guid.NewGuid().ToString();

    public bool IsAvailable => true;

    public void Clear() => _store.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
