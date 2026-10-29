namespace PAQTERIA.Server.Models;

public sealed record CreatePackageRequest(
    string? Folio,
    int? IdCentroOrigen,
    string? DireccionOrigen,
    string? DireccionDestino,
    string? CoordenadasDestino,
    decimal? PesoKg,
    string? TamanoEtiqueta,
    bool EsPrioritario,
    bool EsFragil);

public sealed record PackageResponse(
    int IdPaquete,
    string Folio,
    int IdCentroOrigen,
    string DireccionOrigen,
    string DireccionDestino,
    string CoordenadasDestino,
    decimal PesoKg,
    string TamanoEtiqueta,
    bool EsPrioritario,
    bool EsFragil,
    string EstadoActual,
    DateTime FechaCreacion);
