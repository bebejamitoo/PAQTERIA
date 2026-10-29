using Microsoft.Data.SqlClient;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Data;

public sealed class PackageDb(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se configuró ConnectionStrings:DefaultConnection.");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT OBJECT_ID(N'dbo.PAQUETES', N'U');";
        var tableId = await command.ExecuteScalarAsync(cancellationToken);
        if (tableId is null or DBNull)
        {
            throw new InvalidOperationException("No existe la tabla PAQTERIA.dbo.PAQUETES.");
        }
    }

    public async Task<IReadOnlyList<Package>> GetPackagesAsync(string? search, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id_paquete, folio, id_centro_origen,
                   direccion_origen, direccion_destino, coordenadas_destino,
                   peso_kg, tamano_etiqueta, es_prioritario, es_fragil,
                   estado_actual, fecha_creacion
            FROM dbo.PAQUETES
            WHERE @search = N''
               OR folio LIKE @pattern
               OR direccion_destino LIKE @pattern
               OR estado_actual LIKE @pattern
            ORDER BY fecha_creacion DESC;
            """;
        AddSearchParameters(command, search);

        var packages = new List<Package>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            packages.Add(ReadPackage(reader));
        }

        return packages;
    }

    public async Task<Package> CreatePackageAsync(CreatePackageRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.PAQUETES
            (
                folio, id_centro_origen, direccion_origen,
                direccion_destino, coordenadas_destino, peso_kg,
                tamano_etiqueta, es_prioritario, es_fragil,
                estado_actual, fecha_creacion
            )
            OUTPUT INSERTED.id_paquete
            VALUES
            (
                @folio, @idCentroOrigen, @direccionOrigen,
                @direccionDestino, @coordenadasDestino, @pesoKg,
                @tamanoEtiqueta, @esPrioritario, @esFragil,
                N'En clasificación', SYSUTCDATETIME()
            );
            """;
        AddCreateParameters(command, request);

        var idPaquete = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return await GetPackageByIdAsync(idPaquete, connection, cancellationToken)
            ?? throw new InvalidOperationException("El paquete se insertó, pero no pudo recuperarse.");
    }

    public async Task<bool> DistributionCenterExistsAsync(
        int idCentro,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.CENTROS_DISTRIBUCION
            WHERE id_centro = @idCentro;
            """;
        command.Parameters.AddWithValue("@idCentro", idCentro);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value) > 0;
    }

    public async Task<Package?> EnterPackageAsync(string folio, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.PAQUETES
            SET estado_actual = N'En clasificación'
            WHERE folio = @folio;
            """;
        command.Parameters.AddWithValue("@folio", folio);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows == 0
            ? null
            : await GetPackageByFolioAsync(folio, connection, cancellationToken);
    }

    private async Task<Package?> GetPackageByIdAsync(int idPaquete, SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id_paquete, folio, id_centro_origen,
                   direccion_origen, direccion_destino, coordenadas_destino,
                   peso_kg, tamano_etiqueta, es_prioritario, es_fragil,
                   estado_actual, fecha_creacion
            FROM dbo.PAQUETES
            WHERE id_paquete = @idPaquete;
            """;
        command.Parameters.AddWithValue("@idPaquete", idPaquete);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPackage(reader) : null;
    }

    private async Task<Package?> GetPackageByFolioAsync(string folio, SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id_paquete, folio, id_centro_origen,
                   direccion_origen, direccion_destino, coordenadas_destino,
                   peso_kg, tamano_etiqueta, es_prioritario, es_fragil,
                   estado_actual, fecha_creacion
            FROM dbo.PAQUETES
            WHERE folio = @folio;
            """;
        command.Parameters.AddWithValue("@folio", folio);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPackage(reader) : null;
    }

    private static void AddSearchParameters(SqlCommand command, string? search)
    {
        var value = search?.Trim() ?? string.Empty;
        command.Parameters.AddWithValue("@search", value);
        command.Parameters.AddWithValue("@pattern", $"%{value}%");
    }

    private static void AddCreateParameters(SqlCommand command, CreatePackageRequest request)
    {
        command.Parameters.AddWithValue("@folio", request.Folio!.Trim());
        command.Parameters.AddWithValue("@idCentroOrigen", request.IdCentroOrigen!.Value);
        command.Parameters.AddWithValue("@direccionOrigen", request.DireccionOrigen!.Trim());
        command.Parameters.AddWithValue("@direccionDestino", request.DireccionDestino!.Trim());
        command.Parameters.AddWithValue("@coordenadasDestino", request.CoordenadasDestino!.Trim());
        command.Parameters.AddWithValue("@pesoKg", request.PesoKg!.Value);
        command.Parameters.AddWithValue("@tamanoEtiqueta", request.TamanoEtiqueta!.Trim());
        command.Parameters.AddWithValue("@esPrioritario", request.EsPrioritario);
        command.Parameters.AddWithValue("@esFragil", request.EsFragil);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static Package ReadPackage(SqlDataReader reader) => new()
    {
        IdPaquete = reader.GetInt32(0),
        Folio = reader.GetString(1),
        IdCentroOrigen = reader.GetInt32(2),
        DireccionOrigen = reader.GetString(3),
        DireccionDestino = reader.GetString(4),
        CoordenadasDestino = reader.GetString(5),
        PesoKg = reader.GetDecimal(6),
        TamanoEtiqueta = reader.GetString(7),
        EsPrioritario = reader.GetBoolean(8),
        EsFragil = reader.GetBoolean(9),
        EstadoActual = reader.GetString(10),
        FechaCreacion = reader.GetDateTime(11),
    };
}