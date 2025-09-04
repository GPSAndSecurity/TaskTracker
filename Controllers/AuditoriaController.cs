using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Services;

[Route("api/[controller]")]
[ApiController]
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
[Authorize(Roles = "admin_empresa,superadmin,colaborador")]
public async Task<IActionResult> ObtenerNotificaciones()
{
    var usuarioId = ObtenerUsuarioIdDesdeToken();
    var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

    if (roles.Contains("colaborador"))
    {
        var notificaciones = await _auditoriaService.ObtenerNotificacionesParaColaboradorAsync(usuarioId);
        return Ok(notificaciones);
    }
    else
    {
        var empresaId = ObtenerEmpresaIdDesdeToken();
        var notificaciones = await _auditoriaService.ObtenerNotificacionesPorEmpresaAsync(empresaId);
        return Ok(notificaciones);
    }
}

private int ObtenerUsuarioIdDesdeToken()
{
    var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int usuarioId))
        throw new UnauthorizedAccessException("userId inválido en el token");

    return usuarioId;
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
