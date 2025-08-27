using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Services; 
using TaskTracker.DTOs;
using TaskTracker.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ProyectoService _proyectoService;

    public DashboardController(AppDbContext context, ProyectoService proyectoService)
    {
        _context = context;
        _proyectoService = proyectoService;
    }

    [HttpGet("total-colaboradores")]
    public async Task<ActionResult<int>> GetTotalColaboradores()
    {
        int total = await _context.Usuarios.CountAsync(u => u.Rol == "Colaborador");

        return Ok(total);
    }

    [HttpGet("total-empresas")]
    public async Task<ActionResult<int>> GetTotalEmpresas()
    {
        int total = await _context.Empresas.CountAsync();
        return Ok(total);
    }

    [HttpGet("total-clientes")]
    public async Task<ActionResult<int>> GetTotalClientes()
    {
        int total = await _context.Usuarios.CountAsync(u => u.Rol == "Cliente");
        return Ok(total);
    }

    [HttpGet("proyectos-avance")]
    public async Task<ActionResult<List<ProyectoConAvanceDto>>> GetProyectosConAvance([FromQuery] int empresaId)
    {
        var proyectos = await _proyectoService.ObtenerProyectosConAvanceAsync(empresaId);
        return Ok(proyectos);
    }

    [HttpGet("tareas-por-usuario")]
    public async Task<ActionResult<List<TareasPorUsuarioDto>>> GetTareasAgrupadasPorUsuario()
    {
        int empresaId = ObtenerEmpresaIdDesdeToken();

        var query = _context.TareaAsignados
            .Include(ta => ta.Usuario)
            .Include(ta => ta.Tarea)
        .Where(ta => ta.Usuario.EmpresaId.HasValue && ta.Usuario.EmpresaId.Value == empresaId)
            .AsQueryable();

        var result = await query
        .GroupBy(ta => new { ta.UsuarioId, ta.Usuario!.Name, ta.Usuario!.Lastname, ta.Usuario!.EmpresaId })
            .Select(g => new TareasPorUsuarioDto
            {
                UsuarioId = g.Key.UsuarioId,
                UsuarioNombre = g.Key.Name,
                UsuarioApellido = g.Key.Lastname,
                EmpresaId = g.Key.EmpresaId,
                EnProceso = g.Count(ta => ta.Tarea!.Estado == EstadoTarea.EnProceso),
                Finalizadas = g.Count(ta => ta.Tarea!.Estado == EstadoTarea.Finalizada),
                Inconclusas = g.Count(ta => ta.Tarea!.Estado == EstadoTarea.Inconclusa),
                Total = g.Count()
            })
            .ToListAsync();

        return Ok(result);
    }
    private int ObtenerEmpresaIdDesdeToken()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "empresaId")?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int empresaId))
            throw new UnauthorizedAccessException("empresaId inválido en el token");

        return empresaId;
    }
    
    

    [HttpGet("estadisticas-tareas-colaborador")]
[Authorize(Roles = "colaborador")]
public async Task<ActionResult<object>> ObtenerEstadisticasTareasColaborador()
{
    var usuarioId = GetUsuarioIdDesdeToken();
    if (usuarioId == null)
        return Unauthorized("No se pudo obtener el ID del usuario desde el token.");

    var tareasAsignadas = await _context.TareaAsignados
        .Include(ta => ta.Tarea)
        .Where(ta => ta.UsuarioId == usuarioId)
        .ToListAsync();

    var estadisticas = new
    {
        EnProceso = tareasAsignadas.Count(t => t.Tarea!.Estado == EstadoTarea.EnProceso),
        Finalizadas = tareasAsignadas.Count(t => t.Tarea!.Estado == EstadoTarea.Finalizada),
        Inconclusas = tareasAsignadas.Count(t => t.Tarea!.Estado == EstadoTarea.Inconclusa)
    };

    return Ok(estadisticas);
}
private int? GetUsuarioIdDesdeToken()
{
    var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(claim, out int usuarioId) ? usuarioId : null;
}

}
