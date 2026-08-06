namespace WebImagenologia.Web.Models.ApiDtos;

public record UsuarioConexionDto(
    string Usuario,
    string NombreCompleto,
    string Rol,
    IEnumerable<EmpresaDto> EmpresasAsignadas);
