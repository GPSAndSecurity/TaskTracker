using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Services;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "admin_empresa,superadmin")]
public class AuditoriaController : ControllerBase
{
    private readonly AuditoriaService _auditoriaService;

    public AuditoriaController(AuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<List<Auditoria>>> ObtenerLogs([FromQuery] AuditoriaFilterDto filtros)
    {
        var logs = await _auditoriaService.ObtenerLogsAsync(
            filtros.FechaInicio,
            filtros.FechaFin,
            filtros.Accion,
            filtros.UsuarioId);

        return Ok(logs);
    }
}
