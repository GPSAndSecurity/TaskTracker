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
public class TareasController : ControllerBase
{
    private readonly TareaService _tareaService;

    public TareasController(TareaService tareaService)
    {
        _tareaService = tareaService;
    }

    [HttpPost]
    public async Task<ActionResult<Tarea>> CrearTarea(CreateTareaDto dto)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null) return Unauthorized();

        var tarea = await _tareaService.CrearTareaAsync(dto, empresaId.Value);
        if (tarea == null)
            return BadRequest("El proyecto no existe o no pertenece a tu empresa.");

        return CreatedAtAction(nameof(CrearTarea), new { id = tarea.Id }, tarea);
    }

    [HttpGet("por-proyecto/{proyectoId}")]
    public async Task<ActionResult<List<Tarea>>> ObtenerTareasPorProyecto(int proyectoId)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null) return Unauthorized();

        var tareas = await _tareaService.ObtenerTareasPorProyectoAsync(proyectoId, empresaId.Value);
        return Ok(tareas);
    }

    [HttpPost("asignar-colaboradores")]
    public async Task<IActionResult> AsignarColaboradores(int tareaId, [FromBody] List<int> usuarioIds)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null) return Unauthorized();

        var exito = await _tareaService.AsignarColaboradoresATareaAsync(tareaId, usuarioIds, empresaId.Value);
        if (!exito)
            return BadRequest("Tarea no encontrada o usuarios inválidos.");

        return Ok("Colaboradores asignados a la tarea.");
    }

    [HttpPatch("{tareaId}/estado")]
    public async Task<IActionResult> CambiarEstadoTarea(int tareaId, [FromBody] EstadoTarea nuevoEstado)
    {
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null) return Unauthorized();

        var exito = await _tareaService.CambiarEstadoTareaAsync(tareaId, nuevoEstado, empresaId.Value);
        if (!exito)
            return BadRequest("Tarea no encontrada o no pertenece a la empresa.");

        return Ok("Estado de tarea actualizado.");
    }

    private int? GetEmpresaIdFromToken()
    {
        var empresaClaim = User.FindFirst("empresaId")?.Value;
        return int.TryParse(empresaClaim, out var id) ? id : null;
    }
}
