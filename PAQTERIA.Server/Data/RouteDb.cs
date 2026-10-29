using Microsoft.Data.SqlClient;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Data;

public sealed class RouteDb(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se configuró ConnectionStrings:DefaultConnection.");

    public async Task<RouteOptionsResponse> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var drivers = new List<RouteDriverOption>();
        var units = new List<RouteUnitOption>();
        var packages = new List<RoutePackageOption>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id_usuario, nombre, email
                FROM dbo.USUARIOS
                ORDER BY nombre;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                drivers.Add(new RouteDriverOption(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id_unidad, codigo_unidad, placas
                FROM dbo.UNIDADES
                ORDER BY codigo_unidad;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                units.Add(new RouteUnitOption(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.id_paquete, p.folio, p.direccion_destino,
                       ISNULL(p.es_prioritario, 0), p.peso_kg
                FROM dbo.PAQUETES p
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.PARADAS_RUTA pr
                    INNER JOIN dbo.TURNOS_REPARTIDOR tr ON tr.id_turno = pr.id_turno
                    WHERE pr.id_paquete = p.id_paquete
                      AND tr.estado_turno NOT IN (N'Cancelado', N'Finalizado')
                )
                ORDER BY ISNULL(p.es_prioritario, 0) DESC, p.fecha_creacion, p.id_paquete;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                packages.Add(new RoutePackageOption(
                    reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.GetBoolean(3), reader.GetDecimal(4)));
            }
        }

        return new RouteOptionsResponse(drivers, units, packages);
    }

    public async Task<RouteMonitoringResponse> GetMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var turns = new List<RouteMonitoringItem>();
        var totalPackages = 0;
        var pendingStops = 0;
        var openIncidents = 0;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT tr.id_turno, tr.fecha_turno,
                       COALESCE(u.nombre, N'Sin repartidor') AS repartidor,
                       COALESCE(un.codigo_unidad + N' - ' + un.placas, N'Sin unidad') AS unidad,
                       tr.estado_turno,
                       COUNT(pr.id_parada) AS total_paquetes,
                       COALESCE(SUM(CASE WHEN pr.estado_parada NOT IN (N'Entregada', N'Cancelada') THEN 1 ELSE 0 END), 0) AS pendientes,
                       COUNT(DISTINCT ie.id_incidencia) AS incidencias
                FROM dbo.TURNOS_REPARTIDOR tr
                LEFT JOIN dbo.USUARIOS u ON u.id_usuario = tr.id_repartidor
                LEFT JOIN dbo.UNIDADES un ON un.id_unidad = tr.id_unidad
                LEFT JOIN dbo.PARADAS_RUTA pr ON pr.id_turno = tr.id_turno
                LEFT JOIN dbo.INCIDENCIAS_ENTREGA ie ON ie.id_paquete = pr.id_paquete
                GROUP BY tr.id_turno, tr.fecha_turno, u.nombre, un.codigo_unidad, un.placas, tr.estado_turno
                ORDER BY tr.fecha_turno DESC, tr.id_turno DESC;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var item = new RouteMonitoringItem(
                    reader.GetInt32(0), reader.GetFieldValue<DateOnly>(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetInt32(5),
                    reader.GetInt32(6), reader.GetInt32(7), Array.Empty<RouteStopItem>());

                turns.Add(item);
                totalPackages += item.TotalPaquetes;
                pendingStops += item.ParadasPendientes;
                openIncidents += item.Incidencias;
            }
        }

        for (var i = 0; i < turns.Count; i++)
        {
            var turn = turns[i];
            var stops = await GetStopsAsync(connection, turn.IdTurno, cancellationToken);
            turns[i] = turn with { Paradas = stops };
        }

        return new RouteMonitoringResponse(
            turns,
            totalPackages,
            pendingStops,
            openIncidents);
    }

    public async Task<int> CreateRouteAsync(RouteAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureReferencesAsync(connection, transaction, request, cancellationToken);
            await using var turnCommand = new SqlCommand("""
                INSERT INTO dbo.TURNOS_REPARTIDOR
                    (id_repartidor, id_unidad, fecha_turno, estado_turno, total_paquetes, tiempo_estimado_horas)
                OUTPUT INSERTED.id_turno
                VALUES (@idRepartidor, @idUnidad, @fechaTurno, N'Asignado', @totalPaquetes, @tiempoEstimadoHoras);
                """, connection, transaction);
            turnCommand.Parameters.AddWithValue("@idRepartidor", request.IdRepartidor!.Value);
            turnCommand.Parameters.AddWithValue("@idUnidad", request.IdUnidad!.Value);
            turnCommand.Parameters.AddWithValue("@fechaTurno", request.FechaTurno!.Value.ToDateTime(TimeOnly.MinValue));
            turnCommand.Parameters.AddWithValue("@totalPaquetes", request.IdPaquetes!.Count);
            turnCommand.Parameters.AddWithValue("@tiempoEstimadoHoras", request.TiempoEstimadoHoras!.Value);
            var idTurno = Convert.ToInt32(await turnCommand.ExecuteScalarAsync(cancellationToken));

            for (var index = 0; index < request.IdPaquetes.Count; index++)
            {
                var idPaquete = request.IdPaquetes[index];
                await ExecuteAsync(connection, transaction, """
                    INSERT INTO dbo.PARADAS_RUTA (id_turno, id_paquete, orden_secuencia, estado_parada)
                    VALUES (@idTurno, @idPaquete, @orden, N'Pendiente');
                    UPDATE dbo.PAQUETES SET estado_actual = N'En ruta' WHERE id_paquete = @idPaquete;
                    INSERT INTO dbo.HISTORIAL_SEGUIMIENTO (id_paquete, titulo, descripcion, fecha_hora)
                    VALUES (@idPaquete, N'Ruta asignada', @descripcion, SYSUTCDATETIME());
                    """, new Dictionary<string, object?>
                {
                    ["@idTurno"] = idTurno,
                    ["@idPaquete"] = idPaquete,
                    ["@orden"] = index + 1,
                    ["@descripcion"] = $"Asignado al turno {idTurno}, parada {index + 1}."
                }, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return idTurno;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task EnsureReferencesAsync(SqlConnection connection, SqlTransaction transaction, RouteAssignmentRequest request, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM dbo.USUARIOS WHERE id_usuario = @idRepartidor)
                THROW 51000, 'El repartidor no existe.', 1;
            IF NOT EXISTS (SELECT 1 FROM dbo.UNIDADES WHERE id_unidad = @idUnidad)
                THROW 51001, 'La unidad no existe.', 1;
            IF (SELECT COUNT(*) FROM dbo.PAQUETES p WHERE p.id_paquete IN (SELECT value FROM OPENJSON(@ids))) <> @total
                THROW 51002, 'Uno o más paquetes no existen.', 1;
            IF EXISTS
            (
                SELECT 1 FROM dbo.PARADAS_RUTA pr
                INNER JOIN dbo.TURNOS_REPARTIDOR tr ON tr.id_turno = pr.id_turno
                WHERE pr.id_paquete IN (SELECT value FROM OPENJSON(@ids))
                  AND tr.estado_turno NOT IN (N'Cancelado', N'Finalizado')
            )
                THROW 51003, 'Uno o más paquetes ya están asignados a una ruta.', 1;
            """, connection, transaction);
        command.Parameters.AddWithValue("@idRepartidor", request.IdRepartidor!.Value);
        command.Parameters.AddWithValue("@idUnidad", request.IdUnidad!.Value);
        command.Parameters.AddWithValue("@ids", $"[{string.Join(',', request.IdPaquetes!)}]");
        command.Parameters.AddWithValue("@total", request.IdPaquetes.Count);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<RouteStopItem>> GetStopsAsync(SqlConnection connection, int idTurno, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pr.id_parada, pr.id_paquete, p.folio, pr.orden_secuencia,
                   pr.estado_parada, p.direccion_destino, p.estado_actual,
                   COALESCE((SELECT TOP 1 hs.titulo FROM dbo.HISTORIAL_SEGUIMIENTO hs
                             WHERE hs.id_paquete = p.id_paquete ORDER BY hs.fecha_hora DESC, hs.id_historial DESC), N'Sin eventos')
            FROM dbo.PARADAS_RUTA pr
            INNER JOIN dbo.PAQUETES p ON p.id_paquete = pr.id_paquete
            WHERE pr.id_turno = @idTurno
            ORDER BY pr.orden_secuencia;
            """;
        command.Parameters.AddWithValue("@idTurno", idTurno);
        var stops = new List<RouteStopItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stops.Add(new RouteStopItem(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7)));
        }
        return stops;
    }

    private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql, Dictionary<string, object?> values, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        foreach (var value in values) command.Parameters.AddWithValue(value.Key, value.Value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
