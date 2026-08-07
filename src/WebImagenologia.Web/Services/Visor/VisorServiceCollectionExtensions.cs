using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>Registro DI aditivo del módulo Visor (SPEC-002 §4.6).</summary>
public static class VisorServiceCollectionExtensions
{
    public static IServiceCollection AddVisorModule(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<VisorOptions>(config.GetSection("Visor"));
        services.AddScoped<IVisorTokenService, VisorTokenService>();
        services.AddScoped<IVisorAuditoriaService, VisorAuditoriaService>();
        services.AddScoped<IEstudioResolver, EstudioResolver>();
        services.AddHttpClient<IDicomWebClient, DicomWebClient>();
        services.AddHttpClient<IOrthancGatewayService, OrthancGatewayService>();
        return services;
    }
}
