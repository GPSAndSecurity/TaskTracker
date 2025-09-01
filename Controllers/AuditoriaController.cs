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

[HttpGet("notificaciones")]
public async Task<IActionResult> ObtenerNotificaciones()
{
    var empresaId = ObtenerEmpresaIdDesdeToken();
    var notificaciones = await _auditoriaService.ObtenerNotificacionesPorEmpresaAsync(empresaId);
    return Ok(notificaciones);
}


private int ObtenerEmpresaIdDesdeToken()
{
    var claim = User.Claims.FirstOrDefault(c => c.Type == "empresaId")?.Value;
    if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int empresaId))
        throw new UnauthorizedAccessException("empresaId inválido en el token");

    return empresaId;
}

[HttpPatch("marcar-vista/{id}")]
public async Task<IActionResult> MarcarNotificacionComoVista(int id)
{
    var auditoria = await _auditoriaService.MarcarComoVistaAsync(id);

    if (auditoria == null)
        return NotFound();

    return Ok();
}

}
