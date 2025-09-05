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

    [HttpGet("eventos")]
    [Authorize(Roles = "admin_empresa,superadmin,colaborador")]
    public async Task<IActionResult> ObtenerEventosAuditoria([FromQuery] EventoAuditoriaFilterDto filtros)
    {
        var usuarioId = ObtenerUsuarioIdDesdeToken();
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        if (roles.Contains("colaborador"))
        {
            filtros.UsuarioId = usuarioId;
        }

        var eventos = await _auditoriaService.ObtenerEventosAuditoriaAsync(filtros);
        return Ok(eventos);
    }

    [HttpPatch("marcar-vista/{id}")]
    public async Task<IActionResult> MarcarNotificacionComoVista(int id)
    {
        var auditoria = await _auditoriaService.MarcarComoVistaAsync(id);
        if (auditoria == null)
            return NotFound($"No se encontró la notificación con ID {id}");

        return Ok();
    }

    private int ObtenerUsuarioIdDesdeToken()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int usuarioId))
            throw new UnauthorizedAccessException("userId inválido en el token");

        return usuarioId;
    }
}
