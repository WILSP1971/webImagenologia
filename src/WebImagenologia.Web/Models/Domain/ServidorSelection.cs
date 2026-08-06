using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.Domain;

public static class ServidorSelection
{
    public static string BuildKey(ServidorDto servidor) =>
        $"{servidor.IpConexion}|{servidor.BdConexion}|{servidor.PortConexion}";

    public static ServidorDto? ParseKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var parts = key.Split('|', 3);
        if (parts.Length != 3)
        {
            return null;
        }

        var ip = parts[0].Trim();
        var bd = parts[1].Trim();
        var port = parts[2].Trim();

        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        return new ServidorDto(string.Empty, ip, bd, port);
    }
}
