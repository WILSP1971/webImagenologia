using System.Text.Json;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Services;

internal static class ApiFlexibleJson
{
    public static List<DependenciaDto> ParseDependencias(string json)
    {
        var items = ExtractArrayElements(json);
        var dependencias = new List<DependenciaDto>();

        foreach (var item in items)
        {
            var codigo = GetString(item,
                "CodDependencia",
                "Cod_Dependencia",
                "cod_dependencia",
                "Dependencia",
                "Codigo",
                "CodigoDependencia");

            if (string.IsNullOrWhiteSpace(codigo))
            {
                continue;
            }

            var nombre = GetString(item,
                "NomDependecia",
                "NombreDependencia",
                "Nombre_Dependencia",
                "nombre_dependencia",
                "Descripcion",
                "Nombre",
                "DependenciaNombre") ?? codigo;

            dependencias.Add(new DependenciaDto(codigo.Trim(), nombre.Trim()));
        }

        return dependencias;
    }

    public static List<EstudioEmpresaDto> ParseEstudiosEmpresa(string json)
    {
        var items = ExtractArrayElements(json);
        var estudios = new List<EstudioEmpresaDto>();

        foreach (var item in items)
        {
            var codigoEmpresa = GetString(item,
                "CodigoEmpresas",
                "Empresa",
                "CodigoEmpresa",
                "codigo_empresa");

            if (string.IsNullOrWhiteSpace(codigoEmpresa))
            {
                continue;
            }

            var nombreEmpresa = GetString(item,
                "nombre_empresa",
                "NombreEmpresa",
                "Nombre_Empresa",
                "NombreEmpresaDescripcion");

            var codDependencia = GetString(item,
                "CodDependencia",
                "Cod_Dependencia",
                "cod_dependencia",
                "Dependencia") ?? string.Empty;

            var nombreDependencia = GetString(item,
                "NombreDependencia",
                "Nombre_Dependencia",
                "nombre_dependencia",
                "DependenciaNombre") ?? codDependencia;

            var cantidad = GetDecimal(item, "Cantidad") ?? 0;
            var estado = GetString(item, "Estado") ?? string.Empty;

            estudios.Add(new EstudioEmpresaDto(
                codigoEmpresa.Trim(),
                codDependencia.Trim(),
                cantidad,
                estado.Trim(),
                nombreDependencia.Trim(),
                EmpresaDto.FormatoEtiqueta(codigoEmpresa, nombreEmpresa ?? string.Empty)));
        }

        return estudios;
    }

    public static List<MedicoDto> ParseMedicos(string json)
    {
        var items = ExtractArrayElements(json);
        var medicos = new List<MedicoDto>();

        foreach (var item in items)
        {
            var cedula = GetString(item,
                "Cedula",
                "CedulaMedico",
                "Cedula_Medico",
                "cedula",
                "cedula_medico",
                "CEDULA");

            if (string.IsNullOrWhiteSpace(cedula))
            {
                continue;
            }

            var nombre = GetString(item,
                "Nombre",
                "NombreMedico",
                "Nombre_Medico",
                "NombreCompleto",
                "Nombre_Completo",
                "nombre",
                "nombre_medico",
                "nombre_completo",
                "NOMBRE")
                // Último recurso: buscar cualquier propiedad cuyo nombre contenga "nombre"
                ?? FindPropertyContaining(item, "nombre")
                ?? cedula;

            medicos.Add(new MedicoDto(cedula.Trim(), nombre.Trim()));
        }

        return medicos;
    }

    public static List<RadiologoRegistradoDto> ParseRadiologosRegistrados(string json)
    {
        var items = ExtractArrayElements(json);
        var radiologos = new List<RadiologoRegistradoDto>();

        foreach (var item in items)
        {
            var cedula = GetString(item,
                "CedulaMedico",
                "Cedula_Medico",
                "cedula_medico",
                "Cedula",
                "cedula");

            if (string.IsNullOrWhiteSpace(cedula))
            {
                continue;
            }

            var codigoEmpresa = GetString(item,
                "CodigoEmpresas",
                "Empresa",
                "codigoEmp",
                "codigo_empresa",
                "CodigoEmpresa") ?? string.Empty;

            var nombreEmpresa = GetString(item,
                "nombre_empresa",
                "NombreEmpresa",
                "Nombre_Empresa",
                "NombreEmpresaDescripcion");

            var empresas = new List<string>();
            if (!string.IsNullOrWhiteSpace(codigoEmpresa))
            {
                empresas.Add(codigoEmpresa.Trim());
            }

            radiologos.Add(new RadiologoRegistradoDto(
                cedula.Trim(),
                GetString(item,
                    "NombreMedico", "Nombre_Medico", "Nombre", "NombreCompleto",
                    "nombre_medico", "nombre", "NOMBRE") ?? string.Empty,
                GetString(item,
                    "UsuarioEsculapio", "Usuario_Esculapio", "Usuario",
                    "usuario_esculapio", "usuario") ?? string.Empty,
                GetString(item,
                    "CodDependencia", "Cod_Dependencia", "cod_dependencia",
                    "CodDep") ?? string.Empty,
                GetString(item,
                    "NombreDependencia", "Nombre_Dependencia", "NomDependecia",
                    "NomDependencia", "nombre_dependencia") ?? string.Empty,
                GetDecimal(item, "Cantidad", "cantidad") ?? 0,
                empresas,
                EmpresaDto.FormatoEtiqueta(codigoEmpresa, nombreEmpresa ?? string.Empty)));
        }

        return radiologos;
    }

    private static string? FindPropertyContaining(JsonElement element, string keyword)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (!prop.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var val = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetRawText(),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(val))
            {
                return val;
            }
        }

        return null;
    }

    private static List<JsonElement> ExtractArrayElements(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray().Select(e => e.Clone()).ToList();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var propertyName in new[] { "data", "Data", "result", "Result", "items", "Items" })
                {
                    if (TryGetProperty(root, propertyName, out var nested)
                        && nested.ValueKind == JsonValueKind.Array)
                    {
                        return nested.EnumerateArray().Select(e => e.Clone()).ToList();
                    }
                }
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return [];
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetRawText(),
                    _ => null
                };
            }
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String
                && decimal.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
    {
        if (element.TryGetProperty(name, out property))
        {
            return true;
        }

        foreach (var candidate in element.EnumerateObject())
        {
            if (candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}
