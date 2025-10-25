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
    private readonly AuditoriaService _auditoria;  

    public ProyectosColaboradorController(ProyectoService proyectoService, AuditoriaService auditoria)
    {
        _proyectoService = proyectoService;
        _auditoria = auditoria;  
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

    // GET: api/proyectos/{proyectoId}/colaboradores
    [HttpGet("{proyectoId}/colaboradores")]
    
    public async Task<ActionResult<List<UsuarioDto>>> ObtenerColaboradoresPorProyecto(int proyectoId)
    {
        var colaboradores = await _proyectoService.ObtenerColaboradoresPorProyectoAsync(proyectoId);
        return Ok(colaboradores);
    }

    // GET: api/proyectos/{proyectoId}/colaboradores/disponibles
    [HttpGet("{proyectoId}/colaboradores/disponibles")]
    public async Task<ActionResult<List<UsuarioDto>>> ObtenerColaboradoresDisponibles(int proyectoId)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var disponibles = await _proyectoService.ObtenerColaboradoresDisponiblesParaProyectoAsync(proyectoId, empresaId.Value);
        return Ok(disponibles);
    }

    // DELETE: api/proyectos/{proyectoId}/colaboradores/{usuarioId}
    [HttpDelete("{proyectoId}/colaboradores/{usuarioId}")]
    public async Task<IActionResult> EliminarColaboradorDeProyecto(int proyectoId, int usuarioId)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var resultado = await _proyectoService.EliminarColaboradorDeProyectoAsync(proyectoId, usuarioId, empresaId.Value);
        if (!resultado)
            return NotFound("No se encontró el colaborador asignado o no pertenece a la empresa.");

        // Registrar auditoría
        await _auditoria.RegistrarEventoAsync(
            accion: "Eliminar colaborador",
            entidad: "ProyectoColaborador",
            entidadId: proyectoId,
            descripcion: $"Se eliminó al colaborador {usuarioId} del proyecto {proyectoId}",
             generaNotificacion: true
        );

        return NoContent();
    }

    private int? GetUsuarioIdFromToken()
    {
        var usuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(usuarioClaim, out var id) ? id : null;
    }

    private int? GetEmpresaIdFromToken()
    {
        var empresaClaim = User.FindFirst("empresaId")?.Value;
        return int.TryParse(empresaClaim, out var id) ? id : null;
    }
    
    
}