namespace WebImagenologia.Web.Models.Domain;

/// <summary>
/// Normaliza el valor de rol devuelto por la API Esculapio al nombre usado en claims y menú.
/// </summary>
public static class RoleNormalizer
{
    public static string? TryNormalize(string? rolApi)
    {
        if (string.IsNullOrWhiteSpace(rolApi))
        {
            return null;
        }

        var rol = rolApi.Trim();

        if (Matches(rol,
                "Administrador",
                "Admin",
                "ADMIN",
                "ADMINISTRADOR",
                "Administrador del Sistema",
                "Administrador del sistema",
                "Usuario Administrador",
                "Super Administrador"))
        {
            return RoleNames.Administrador;
        }

        if (Matches(rol, "Radiologo", "Radiólogo", "RADIOLOGO", "Medico", "Médico", "MEDICO", "Doctor", "MEDICO RADIOLOGO"))
        {
            return RoleNames.Radiologo;
        }

        if (Matches(rol, "Operador", "OPERADOR", "Operator"))
        {
            return RoleNames.Operador;
        }

        return null;
    }

    private static bool Matches(string value, params string[] candidates) =>
        candidates.Any(c => value.Equals(c, StringComparison.OrdinalIgnoreCase));
}
