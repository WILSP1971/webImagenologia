using System.Globalization;

namespace WebImagenologia.Web.Models.ApiDtos;

public record RegistrarEstudioEmpresaRequest(
    string Empresa,
    string codDependencia,
    decimal Cantidad,
    string Estado,
    string tipo);
