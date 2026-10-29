namespace PAQTERIA.Server.Models;

public sealed record RouteAssignmentRequest(
    int? IdRepartidor,
    int? IdUnidad,
    DateOnly? FechaTurno,
    decimal? TiempoEstimadoHoras,
    IReadOnlyList<int>? IdPaquetes);

public sealed record RouteOptionsResponse(
    IReadOnlyList<RouteDriverOption> Repartidores,
    IReadOnlyList<RouteUnitOption> Unidades,
    IReadOnlyList<RoutePackageOption> PaquetesDisponibles);

public sealed record RouteDriverOption(int IdRepartidor, string Nombre, string Email);

public sealed record RouteUnitOption(int IdUnidad, string CodigoUnidad, string Placas);

public sealed record RoutePackageOption(
    int IdPaquete,
    string Folio,
    string DireccionDestino,
    bool EsPrioritario,
    decimal PesoKg);

public sealed record RouteMonitoringResponse(
    IReadOnlyList<RouteMonitoringItem> Turnos,
    int PaquetesEnRuta,
    int ParadasPendientes,
    int IncidenciasAbiertas);

public sealed record RouteMonitoringItem(
    int IdTurno,
    DateOnly FechaTurno,
    string Repartidor,
    string Unidad,
    string EstadoTurno,
    int TotalPaquetes,
    int ParadasPendientes,
    int Incidencias,
    IReadOnlyList<RouteStopItem> Paradas);

public sealed record RouteStopItem(
    int IdParada,
    int IdPaquete,
    string Folio,
    int OrdenSecuencia,
    string EstadoParada,
    string DireccionDestino,
    string EstadoPaquete,
    string UltimoEvento);

public sealed record RouteAssignmentResponse(int IdTurno, int TotalPaquetes, string Message);
