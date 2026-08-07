using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Emite y valida tokens efímeros firmados (HMAC-SHA256) que ligan usuario + estudio.
/// El token es autocontenido (stateless): no requiere BD ni caché.
/// Formato: base64url(payloadJson) ~ base64url(hmacSha256(payloadJson, TokenSecret)).
/// </summary>
public interface IVisorTokenService
{
    /// <summary>Emite un token firmado a partir del payload dado.</summary>
    string Emitir(TokenPayload payload);

    /// <summary>
    /// Valida firma (tiempo constante) y expiración. Devuelve <c>true</c> y el payload
    /// decodificado cuando el token es válido; <c>false</c> en cualquier otro caso
    /// (formato inválido, firma alterada o token expirado).
    /// </summary>
    bool TryValidar(string token, out TokenPayload? payload);
}
