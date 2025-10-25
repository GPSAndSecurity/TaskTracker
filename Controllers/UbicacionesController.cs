using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UbicacionesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UbicacionesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ubicacion>>> GetUbicaciones()
    {
        return await _context.Ubicaciones.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ubicacion>> GetUbicacion(int id)
    {
        var ubicacion = await _context.Ubicaciones.FindAsync(id);
        if (ubicacion == null)
            return NotFound();

        return ubicacion;
    }

    [HttpPost]
    public async Task<ActionResult<Ubicacion>> CrearUbicacion(Ubicacion ubicacion)
    {
        _context.Ubicaciones.Add(ubicacion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUbicacion), new { id = ubicacion.Id }, ubicacion);
    }

    [HttpDelete("{id}")]
public async Task<IActionResult> DeleteUbicacion(int id)
{
    var ubicacion = await _context.Ubicaciones.FindAsync(id);
    if (ubicacion == null)
    {
        return NotFound();
    }

    // Opcional: validar si la ubicación está siendo usada en alguna tarea
    var tareasConUbicacion = await _context.Tareas.AnyAsync(t => t.UbicacionId == id);
    if (tareasConUbicacion)
    {
        return BadRequest("No se puede eliminar la ubicación porque está asignada a una o más tareas.");
    }

    _context.Ubicaciones.Remove(ubicacion);
    await _context.SaveChangesAsync();

    return NoContent(); 
}
// GET api/ubicaciones/empresa/{empresaId}
[HttpGet("empresa/{empresaId}")]
public async Task<ActionResult<IEnumerable<Ubicacion>>> GetUbicacionesPorEmpresa(int empresaId)
{
    var ubicaciones = await _context.Ubicaciones
        .Where(u => u.EmpresaId == empresaId)
        .ToListAsync();

    return Ok(ubicaciones);
}

}

