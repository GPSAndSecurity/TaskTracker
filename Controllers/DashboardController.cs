using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Services; 
using TaskTracker.DTOs; 

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
}
