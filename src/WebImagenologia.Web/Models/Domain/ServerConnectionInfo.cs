namespace WebImagenologia.Web.Models.Domain;

public record ServerConnectionInfo(
    string IpConexion,
    string BdConexion,
    string PortConexion,
    string Usuario,
    string Password);
