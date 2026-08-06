using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Tests;

public class RoleNormalizerTests
{
    [Theory]
    [InlineData("Administrador", RoleNames.Administrador)]
    [InlineData("admin", RoleNames.Administrador)]
    [InlineData("ADMINISTRADOR", RoleNames.Administrador)]
    [InlineData("Administrador del Sistema", RoleNames.Administrador)]
    [InlineData("Radiologo", RoleNames.Radiologo)]
    [InlineData("Radiólogo", RoleNames.Radiologo)]
    [InlineData("Medico", RoleNames.Radiologo)]
    [InlineData("Operador", RoleNames.Operador)]
    public void TryNormalize_MapsKnownRoles(string input, string expected) =>
        Assert.Equal(expected, RoleNormalizer.TryNormalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Invitado")]
    public void TryNormalize_UnknownOrEmpty_ReturnsNull(string? input) =>
        Assert.Null(RoleNormalizer.TryNormalize(input));
}
