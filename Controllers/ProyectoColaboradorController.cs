using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Services;
using System.Security.Claims;

[ApiController]
[Route("api/proyectos")]
public class ProyectosColaboradorController : ControllerBase
{
    private readonly ProyectoService _proyectoService;

    public ProyectosColaboradorController(ProyectoService proyectoService)
    {
        _proyectoService = proyectoService;
    }

    // GET: api/proyectos/asignados
    [HttpGet("asignados")]
    [Authorize(Roles = "colaborador")]  // O solo [Authorize] si quieres que cualquiera autenticado acceda
    public async Task<ActionResult<List<ProyectoConTareasDto>>> ObtenerProyectosAsignados()
    {
        var usuarioId = GetUsuarioIdFromToken();
        if (usuarioId == null)
            return Unauthorized("Usuario no identificado en el token.");

        var proyectos = await _proyectoService.ObtenerProyectosAsignadosAColaboradorAsync(usuarioId.Value);
        return Ok(proyectos);
    }

    private int? GetUsuarioIdFromToken()
    {
        var usuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(usuarioClaim, out var id) ? id : null;
    }
}
