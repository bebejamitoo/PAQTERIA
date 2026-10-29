using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/incidents")]
[Authorize(Roles = "Admin")]
public sealed class IncidentsController(IncidentDb incidentDb) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentResponse>>> GetIncidents(
        CancellationToken cancellationToken)
    {
        var incidents = await incidentDb.GetIncidentsAsync(cancellationToken);
        return Ok(incidents.Select(ToResponse).ToList());
    }

    [HttpGet("options")]
    public async Task<ActionResult<IncidentOptionsResponse>> GetOptions(
        CancellationToken cancellationToken)
    {
        return Ok(await incidentDb.GetOptionsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<IncidentResponse>> CreateIncident(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            var incident = await incidentDb.CreateIncidentAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ToResponse(incident));
        }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number == 547)
        {
            return BadRequest(new { message = "El paquete o repartidor seleccionado no existe." });
        }
    }

    private static string? Validate(CreateIncidentRequest request)
    {
        if (request.IdPaquete is null or <= 0) return "Selecciona un paquete.";
        if (request.IdRepartidor is null or <= 0) return "Selecciona un repartidor.";
        if (string.IsNullOrWhiteSpace(request.TipoIncidencia)) return "La descripción de la incidencia es obligatoria.";
        return null;
    }

    private static IncidentResponse ToResponse(DeliveryIncident incident) => new(
        incident.IdIncidencia,
        incident.IdPaquete,
        incident.IdRepartidor,
        incident.TipoIncidencia,
        incident.Comentario,
        incident.UrlFotoReporte,
        incident.FechaHora,
        incident.Folio,
        incident.Repartidor);
}
