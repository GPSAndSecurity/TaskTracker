using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;


[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("total-colaboradores")]
    public async Task<ActionResult<int>> GetTotalColaboradores()
    {
        // Filtra solo usuarios con rol colaborador
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
        // Filtra solo usuarios con rol cliente
        int total = await _context.Usuarios.CountAsync(u => u.Rol == "Cliente");
        return Ok(total);
    }
}
