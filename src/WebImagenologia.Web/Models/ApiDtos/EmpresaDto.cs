namespace WebImagenologia.Web.Models.ApiDtos;

public record EmpresaDto(
    string Codigo,
    string Nombre)
{
    public string Etiqueta => FormatoEtiqueta(Codigo, Nombre);

    public static string FormatoEtiqueta(string codigo, string nombre)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return nombre?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return codigo.Trim();
        }

        return $"{codigo.Trim()} - {nombre.Trim()}";
    }
}
