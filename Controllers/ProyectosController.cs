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

    public ProyectosController(ProyectoService proyectoService)
    {
        _proyectoService = proyectoService;
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
        return CreatedAtAction(nameof(ObtenerProyectos), new { id = proyecto.Id }, proyecto);
    }

    // DELETE: api/proyectos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarProyecto(int id)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var exito = await _proyectoService.EliminarProyectoAsync(id, empresaId.Value);
        if (!exito)
            return NotFound("Proyecto no encontrado o no pertenece a tu empresa.");

        return NoContent();
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

    // total de proyectos por empresa
    [HttpGet("total")]
    public async Task<ActionResult<int>> GetTotalProyectos()
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        int total = await _proyectoService.ContarProyectosPorEmpresaAsync(empresaId.Value);
        return Ok(total);
    }

    private int? GetEmpresaIdFromToken()
    {
        var empresaClaim = User.FindFirst("empresaId")?.Value;
        return int.TryParse(empresaClaim, out var id) ? id : null;
    }

    [HttpGet("con-avance")]
    public async Task<ActionResult<IEnumerable<ProyectoConAvanceDto>>> GetProyectosConAvance()
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada.");

        var proyectos = await _proyectoService.ObtenerProyectosConAvanceAsync(empresaId.Value);
        return Ok(proyectos);
    }
    private int? GetUsuarioIdFromToken()
    {
        var usuarioClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(usuarioClaim, out var id) ? id : null;
    }

    [HttpPut("{id}/archivar")]
    [Authorize(Roles = "superadmin,admin_empresa")]
    public async Task<IActionResult> ArchivarProyecto(int id)
    {
        var resultado = await _proyectoService.ArchivarProyectoAsync(id);
        if (!resultado)
            return BadRequest("Proyecto no encontrado, ya archivado o no se puede archivar.");

        return Ok("Proyecto archivado o eliminado correctamente.");
    }


[HttpPut("{proyectoId}/desarchivar")]
public async Task<IActionResult> DesarchivarProyecto(int proyectoId)
{
    var resultado = await _proyectoService.DesarchivarProyectoAsync(proyectoId);

    if (!resultado)
        return BadRequest("No se pudo desarchivar el proyecto.");

    return Ok("Proyecto desarchivado correctamente.");
}


}
