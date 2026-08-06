using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Services;

public interface ISessionService
{
    void GuardarConnectionString(string connectionString);

    string? ObtenerConnectionString();

    void GuardarUsuario(UsuarioConexionDto usuario);

    UsuarioConexionDto? ObtenerUsuario();

    void LimpiarSesion();
}
