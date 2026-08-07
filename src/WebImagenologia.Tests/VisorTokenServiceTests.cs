using Microsoft.Extensions.Configuration;
using WebImagenologia.Web.Models.Visor;
using WebImagenologia.Web.Services.Visor;

namespace WebImagenologia.Tests;

public class VisorTokenServiceTests
{
    private const string ValidSecret = "0123456789abcdef0123456789abcdef"; // 33 chars, >= 32

    private static VisorTokenService CreateService(string? secret = ValidSecret)
    {
        var configValues = new Dictionary<string, string?>();
        if (secret is not null)
        {
            configValues["Visor:TokenSecret"] = secret;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new VisorTokenService(configuration);
    }

    private static TokenPayload BuildPayload(long issuedAtUnix, long expiresAtUnix) => new()
    {
        Usuario = "jperez",
        Cedula = "123456789",
        StudyInstanceUID = "1.2.840.113619.2.55.3.1234567890",
        IssuedAtUnix = issuedAtUnix,
        ExpiresAtUnix = expiresAtUnix,
        Nonce = Guid.NewGuid().ToString("N")
    };

    [Fact]
    public void Constructor_DoesNotThrowWhenTokenSecretMissing()
    {
        // El constructor NO debe lanzar: VisorTokenService es AddScoped e inyectado en el
        // constructor de VisorController, que atiende endpoints (p. ej. Resolver) que no
        // dependen del token. La falta de secreto se señala de forma perezosa (ver tests
        // de Emitir/TryValidar más abajo), no al construir el servicio.
        var service = CreateService(secret: null);

        Assert.False(service.EstaConfigurado);
    }

    [Fact]
    public void Constructor_DoesNotThrowWhenTokenSecretTooShort()
    {
        var service = CreateService(secret: "corto");

        Assert.False(service.EstaConfigurado);
    }

    [Fact]
    public void Constructor_MarksConfiguredWhenSecretValid()
    {
        var service = CreateService();

        Assert.True(service.EstaConfigurado);
    }

    [Fact]
    public void Emitir_ThrowsVisorNoConfiguradoException_WhenSecretMissing()
    {
        var service = CreateService(secret: null);
        var payload = BuildPayload(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());

        Assert.Throws<VisorNoConfiguradoException>(() => service.Emitir(payload));
    }

    [Fact]
    public void TryValidar_ThrowsVisorNoConfiguradoException_WhenSecretMissing()
    {
        var service = CreateService(secret: null);

        Assert.Throws<VisorNoConfiguradoException>(() => service.TryValidar("cualquier-token", out _));
    }

    [Fact]
    public void Emitir_ProducesTokenWithExpectedFormat()
    {
        var service = CreateService();
        var payload = BuildPayload(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());

        var token = service.Emitir(payload);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(2, token.Split('~').Length);
    }

    [Fact]
    public void TryValidar_AcceptsValidToken()
    {
        var service = CreateService();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = BuildPayload(now, now + 600);

        var token = service.Emitir(payload);

        var isValid = service.TryValidar(token, out var decoded);

        Assert.True(isValid);
        Assert.NotNull(decoded);
        Assert.Equal(payload.Usuario, decoded!.Usuario);
        Assert.Equal(payload.StudyInstanceUID, decoded.StudyInstanceUID);
        Assert.Equal(payload.Cedula, decoded.Cedula);
    }

    [Fact]
    public void TryValidar_RejectsTokenWithTamperedSignature()
    {
        var service = CreateService();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = BuildPayload(now, now + 600);
        var token = service.Emitir(payload);

        var partes = token.Split('~');
        var firmaAlterada = partes[1].Length > 0
            ? (partes[1][0] == 'A' ? 'B' : 'A') + partes[1][1..]
            : "AAAA";
        var tokenAlterado = $"{partes[0]}~{firmaAlterada}";

        var isValid = service.TryValidar(tokenAlterado, out var decoded);

        Assert.False(isValid);
        Assert.Null(decoded);
    }

    [Fact]
    public void TryValidar_RejectsExpiredToken()
    {
        var service = CreateService();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = BuildPayload(now - 1200, now - 600); // expiró hace 10 minutos

        var token = service.Emitir(payload);

        var isValid = service.TryValidar(token, out var decoded);

        Assert.False(isValid);
        Assert.Null(decoded);
    }

    [Fact]
    public void TryValidar_RejectsMalformedToken()
    {
        var service = CreateService();

        Assert.False(service.TryValidar("no-tiene-separador", out _));
        Assert.False(service.TryValidar("", out _));
        Assert.False(service.TryValidar("a~b~c", out _));
    }
}
