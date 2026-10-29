using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAQTERIA.Server.Data;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Controllers;

[ApiController]
[Route("api/distribution-centers")]
[Authorize(Roles = "Admin")]
public sealed class DistributionCentersController(DistributionCenterDb centerDb) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DistributionCenterResponse>>> GetCenters(
        CancellationToken cancellationToken)
    {
        var centers = await centerDb.GetCentersAsync(cancellationToken);
        return Ok(centers.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<DistributionCenterResponse>> CreateCenter(
        [FromBody] CreateDistributionCenterRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var center = await centerDb.CreateCenterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ToResponse(center));
    }

    private static string? Validate(CreateDistributionCenterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre)) return "El nombre del centro es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.Ciudad)) return "La ciudad del centro es obligatoria.";
        if (string.IsNullOrWhiteSpace(request.Direccion)) return "La dirección del centro es obligatoria.";
        return null;
    }

    private static DistributionCenterResponse ToResponse(DistributionCenter center) => new(
        center.IdCentro,
        center.Nombre,
        center.Ciudad,
        center.Direccion);
}
