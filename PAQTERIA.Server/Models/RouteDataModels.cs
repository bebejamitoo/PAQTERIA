namespace PAQTERIA.Server.Models;

public sealed record RouteDriverRow(int IdRepartidor, string Nombre, string Email);

public sealed record RouteUnitRow(int IdUnidad, string CodigoUnidad, string Placas);

public sealed record RoutePackageRow(int IdPaquete, string Folio, string DireccionDestino, bool EsPrioritario, decimal PesoKg);

public sealed record RouteTurnRow(
    int IdTurno,
    DateOnly FechaTurno,
    string Repartidor,
    string Unidad,
    string EstadoTurno,
    int TotalPaquetes,
    int ParadasPendientes,
    int Incidencias,
    IReadOnlyList<RouteStopItem> Paradas);
