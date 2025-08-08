using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.Controllers;

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

    // Crear un proyecto (admin_empresa o superadmin)
    [HttpPost]
    public async Task<ActionResult<Proyecto>> CrearProyecto(CreateProyectoDto dto)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var proyecto = await _proyectoService.CrearProyectoAsync(dto, empresaId.Value);
        return CreatedAtAction(nameof(ObtenerProyectos), new { id = proyecto.Id }, proyecto);
    }

    // Obtener todos los proyectos de la empresa
    [HttpGet]
    public async Task<ActionResult<List<Proyecto>>> ObtenerProyectos()
    {
        var empresaId = GetEmpresaIdFromToken();
        Console.WriteLine($"[DEBUG] empresaId desde token: {empresaId}");
        if (empresaId == null)
            return Unauthorized("Empresa no identificada en el token.");

        var proyectos = await _proyectoService.ObtenerProyectosPorEmpresaAsync(empresaId.Value);
        return Ok(proyectos);
    }

    private int? GetEmpresaIdFromToken()
    {
        var empresaClaim = User.FindFirst("empresaId")?.Value;
        return int.TryParse(empresaClaim, out var id) ? id : null;
    }

    // asignar colaboradores al proyecto
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
    
[HttpDelete("{id}")]
public async Task<IActionResult> EliminarProyecto(int id)
{
    var empresaId = GetEmpresaIdFromToken();
    if (empresaId == null)
        return Unauthorized("Empresa no identificada en el token.");

    Console.WriteLine($"Intentando eliminar proyecto {id} de empresa {empresaId}");

    var exito = await _proyectoService.EliminarProyectoAsync(id, empresaId.Value);
    if (!exito)
    {
        Console.WriteLine($"No se encontró proyecto {id} o no pertenece a empresa {empresaId}");
        return NotFound("Proyecto no encontrado o no pertenece a tu empresa.");
    }

    Console.WriteLine($"Proyecto {id} eliminado correctamente");
    return NoContent(); // 204 sin contenido
}


}
