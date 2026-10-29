namespace PAQTERIA.Server.Models;

public sealed class DistributionCenter
{
    public int IdCentro { get; init; }
    public required string Nombre { get; init; }
    public required string Ciudad { get; init; }
    public required string Direccion { get; init; }
}

public sealed record CreateDistributionCenterRequest(
    string? Nombre,
    string? Ciudad,
    string? Direccion);

public sealed record DistributionCenterResponse(
    int IdCentro,
    string Nombre,
    string Ciudad,
    string Direccion);
