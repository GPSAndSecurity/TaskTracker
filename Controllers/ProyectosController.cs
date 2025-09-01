using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin_empresa,superadmin")]
public class ProyectosController : ControllerBase
{
    private readonly ProyectoService _proyectoService;
    private readonly AuditoriaService _auditoria;

    public ProyectosController(ProyectoService proyectoService, AuditoriaService auditoria)
    {
        _proyectoService = proyectoService;
        _auditoria = auditoria;
    }

    // GET: api/proyectos
    [HttpGet]
    public async Task<ActionResult<List<Proyecto>>> ObtenerProyectos()
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var proyectos = await _proyectoService.ObtenerProyectosPorEmpresaAsync(empresaId.Value);
        return Ok(proyectos);
    }

    // POST: api/proyectos
    [HttpPost]
    public async Task<ActionResult<Proyecto>> CrearProyecto(CreateProyectoDto dto)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var proyecto = await _proyectoService.CrearProyectoAsync(dto, empresaId.Value);

        await _auditoria.RegistrarEventoAsync(
            accion: "Crear Proyecto",
            entidad: "Proyecto",
            entidadId: proyecto.Id,
            descripcion: $"Se creó el proyecto '{proyecto.Nombre}'",
             generaNotificacion: true
        );

        return CreatedAtAction(nameof(ObtenerProyectos), new { id = proyecto.Id }, proyecto);
    }

    // DELETE: api/proyectos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarProyecto(int id)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var proyecto = await _proyectoService.ObtenerPorIdAsync(id); // Asegúrate de tener este método en el servicio
        if (proyecto == null || proyecto.EmpresaId != empresaId)
            return NotFound("Proyecto no encontrado o no pertenece a tu empresa.");

        var exito = await _proyectoService.EliminarProyectoAsync(id, empresaId.Value);
        if (!exito)
            return NotFound("No se pudo eliminar el proyecto.");

        await _auditoria.RegistrarEventoAsync(
            accion: "Eliminar Proyecto",
            entidad: "Proyecto",
            entidadId: id,
            descripcion: $"Se eliminó el proyecto '{proyecto.Nombre}'",
             generaNotificacion: true
        );

        return NoContent();
    }

    // PUT: api/proyectos/{id}/archivar
    [HttpPut("{id}/archivar")]
    public async Task<IActionResult> ArchivarProyecto(int id)
    {
        var proyecto = await _proyectoService.ObtenerPorIdAsync(id);
        if (proyecto == null)
            return NotFound("Proyecto no encontrado.");

        var resultado = await _proyectoService.ArchivarProyectoAsync(id);
        if (!resultado)
            return BadRequest("Ya archivado o no se puede archivar.");

        await _auditoria.RegistrarEventoAsync(
            accion: "Archivar Proyecto",
            entidad: "Proyecto",
            entidadId: id,
            descripcion: $"Se archivó el proyecto '{proyecto.Nombre}'",
             generaNotificacion: true
        );

        return Ok("Proyecto archivado correctamente.");
    }

    // PUT: api/proyectos/{id}/desarchivar
    [HttpPut("{proyectoId}/desarchivar")]
    public async Task<IActionResult> DesarchivarProyecto(int proyectoId)
    {
        var proyecto = await _proyectoService.ObtenerPorIdAsync(proyectoId);
        if (proyecto == null)
            return NotFound("Proyecto no encontrado.");

        var resultado = await _proyectoService.DesarchivarProyectoAsync(proyectoId);
        if (!resultado)
            return BadRequest("No se pudo desarchivar el proyecto.");

        await _auditoria.RegistrarEventoAsync(
            accion: "Desarchivar Proyecto",
            entidad: "Proyecto",
            entidadId: proyectoId,
            descripcion: $"Se desarchivó el proyecto '{proyecto.Nombre}'",
             generaNotificacion: true
        );

        return Ok("Proyecto desarchivado correctamente.");
    }

    // POST: api/proyectos/asignar-colaboradores
    [HttpPost("asignar-colaboradores")]
    public async Task<IActionResult> AsignarColaboradores([FromBody] AsignarColaboradoresProyectoDto dto)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada.");

        var exito = await _proyectoService.AsignarColaboradoresAsync(empresaId.Value, dto);
        if (!exito)
            return BadRequest("Proyecto no encontrado o usuarios inválidos.");

        return Ok("Colaboradores asignados correctamente.");
    }

    // GET: api/proyectos/total
    [HttpGet("total")]
    public async Task<ActionResult<int>> GetTotalProyectos()
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        int total = await _proyectoService.ContarProyectosPorEmpresaAsync(empresaId.Value);
        return Ok(total);
    }

    // GET: api/proyectos/con-avance
    [HttpGet("con-avance")]
    public async Task<ActionResult<IEnumerable<ProyectoConAvanceDto>>> GetProyectosConAvance()
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada.");

        var proyectos = await _proyectoService.ObtenerProyectosConAvanceAsync(empresaId.Value);
        return Ok(proyectos);
    }

    // === MÉTODOS AUXILIARES ===

    private int? GetEmpresaIdFromToken()
    {
        var empresaClaim = User.FindFirst("empresaId")?.Value;
        return int.TryParse(empresaClaim, out var id) ? id : null;
    }

    private int? GetUsuarioIdFromToken()
    {
        var usuarioClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(usuarioClaim, out var id) ? id : null;
    }
}
