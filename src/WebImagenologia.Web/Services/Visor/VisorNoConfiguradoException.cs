namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Señala que el módulo Visor no puede operar porque falta configuración obligatoria
/// (p. ej. <c>Visor:TokenSecret</c>). Se lanza de forma perezosa, únicamente cuando se
/// intenta usar la funcionalidad afectada (emitir/validar token), para no tumbar endpoints
/// del broker que no dependen de dicha configuración (p. ej. <c>Resolver</c>).
/// </summary>
public sealed class VisorNoConfiguradoException : Exception
{
    public VisorNoConfiguradoException(string message) : base(message)
    {
    }
}
