using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <inheritdoc cref="IVisorTokenService"/>
public sealed class VisorTokenService : IVisorTokenService
{
    private const int MinSecretLength = 32;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    private readonly string _tokenSecret;
    private readonly bool _configurado;

    public VisorTokenService(IConfiguration configuration)
    {
        _tokenSecret = configuration["Visor:TokenSecret"] ?? "";
        _configurado = !string.IsNullOrWhiteSpace(_tokenSecret) && _tokenSecret.Length >= MinSecretLength;

        // Deliberadamente NO se lanza aquí: este servicio es AddScoped e inyectado en el
        // constructor de VisorController, que atiende los 5 endpoints del broker (incluidos
        // los que no usan token, p. ej. Resolver). Lanzar en el constructor tumbaría TODO el
        // controlador con un 500 críptico mientras el secreto no esté configurado. La
        // validación se hace de forma perezosa en Emitir/TryValidar (ver EnsureConfigurado).
    }

    /// <summary>Indica si el servicio tiene un <c>Visor:TokenSecret</c> válido configurado.</summary>
    public bool EstaConfigurado => _configurado;

    private void EnsureConfigurado()
    {
        if (!_configurado)
        {
            throw new VisorNoConfiguradoException(
                $"Visor:TokenSecret no está configurado o tiene menos de {MinSecretLength} caracteres. " +
                "Configúrelo mediante User-Secrets (dev) o variable de entorno (producción), nunca en appsettings.json.");
        }
    }

    public string Emitir(TokenPayload payload)
    {
        EnsureConfigurado();

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadPart = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var firma = Base64UrlEncode(Firmar(payloadPart));

        // Separador '~' (carácter no reservado en URLs) para evitar que IIS
        // interprete el segmento como archivo por el punto.
        return $"{payloadPart}~{firma}";
    }

    public bool TryValidar(string token, out TokenPayload? payload)
    {
        EnsureConfigurado();

        payload = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var partes = token.Split('~');
        if (partes.Length != 2)
        {
            return false;
        }

        var payloadPart = partes[0];
        var firmaRecibida = partes[1];

        var firmaEsperada = Base64UrlEncode(Firmar(payloadPart));

        var firmaRecibidaBytes = Encoding.ASCII.GetBytes(firmaRecibida);
        var firmaEsperadaBytes = Encoding.ASCII.GetBytes(firmaEsperada);

        // Comparación en tiempo constante para evitar ataques de temporización.
        // Se comparan longitudes con FixedTimeEquals evitando ramas dependientes de dato.
        if (firmaRecibidaBytes.Length != firmaEsperadaBytes.Length)
        {
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(firmaRecibidaBytes, firmaEsperadaBytes))
        {
            return false;
        }

        TokenPayload? decoded;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payloadPart));
            decoded = JsonSerializer.Deserialize<TokenPayload>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or DecoderFallbackException)
        {
            return false;
        }

        if (decoded is null)
        {
            return false;
        }

        var ahora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (ahora > decoded.ExpiresAtUnix)
        {
            return false;
        }

        payload = decoded;
        return true;
    }

    private byte[] Firmar(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_tokenSecret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
        }

        return Convert.FromBase64String(normalized);
    }
}
