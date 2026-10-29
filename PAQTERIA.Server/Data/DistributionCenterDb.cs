using Microsoft.Data.SqlClient;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Data;

public sealed class DistributionCenterDb(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se configuró ConnectionStrings:DefaultConnection.");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT OBJECT_ID(N'dbo.CENTROS_DISTRIBUCION', N'U');";
        var tableId = await command.ExecuteScalarAsync(cancellationToken);
        if (tableId is null or DBNull)
        {
            throw new InvalidOperationException("No existe la tabla PAQTERIA.dbo.CENTROS_DISTRIBUCION.");
        }
    }

    public async Task<IReadOnlyList<DistributionCenter>> GetCentersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id_centro, nombre, ciudad, direccion
            FROM dbo.CENTROS_DISTRIBUCION
            ORDER BY id_centro;
            """;

        var centers = new List<DistributionCenter>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            centers.Add(ReadCenter(reader));
        }

        return centers;
    }

    public async Task<DistributionCenter> CreateCenterAsync(
        CreateDistributionCenterRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.CENTROS_DISTRIBUCION (nombre, ciudad, direccion)
            OUTPUT INSERTED.id_centro
            VALUES (@nombre, @ciudad, @direccion);
            """;
        command.Parameters.AddWithValue("@nombre", request.Nombre!.Trim());
        command.Parameters.AddWithValue("@ciudad", request.Ciudad!.Trim());
        command.Parameters.AddWithValue("@direccion", request.Direccion!.Trim());

        var idCentro = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return await GetCenterByIdAsync(idCentro, connection, cancellationToken)
            ?? throw new InvalidOperationException("El centro se insertó, pero no pudo recuperarse.");
    }

    private static async Task<DistributionCenter?> GetCenterByIdAsync(
        int idCentro,
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id_centro, nombre, ciudad, direccion
            FROM dbo.CENTROS_DISTRIBUCION
            WHERE id_centro = @idCentro;
            """;
        command.Parameters.AddWithValue("@idCentro", idCentro);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadCenter(reader) : null;
    }

    private static DistributionCenter ReadCenter(SqlDataReader reader) => new()
    {
        IdCentro = reader.GetInt32(0),
        Nombre = reader.GetString(1),
        Ciudad = reader.GetString(2),
        Direccion = reader.GetString(3),
    };

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
