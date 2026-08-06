namespace WebImagenologia.Web.Models.Domain;

public static class RoleNames
{
    public const string Administrador = "Administrador";
    public const string Radiologo = "Radiologo";
    public const string Operador = "Operador";

    /// <summary>Roles combinados para atributos [Authorize(Roles = ...)].</summary>
    public static class Policies
    {
        public const string AdministradorOrRadiologo = $"{Administrador},{Radiologo}";
        public const string AdministradorOrOperador = $"{Administrador},{Operador}";
    }
}
