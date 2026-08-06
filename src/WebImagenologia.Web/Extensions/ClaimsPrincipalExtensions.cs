using System.Security.Claims;

namespace WebImagenologia.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Name)?.Value
        ?? user.Identity?.Name
        ?? "Usuario";

    public static string GetLoginName(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    public static string GetInitials(this ClaimsPrincipal user)
    {
        var name = user.GetDisplayName();
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0].ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
