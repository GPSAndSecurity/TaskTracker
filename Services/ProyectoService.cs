using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Services;

public class ProyectoService
{
    private readonly AppDbContext _context;

    public ProyectoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Proyecto> CrearProyectoAsync(CreateProyectoDto dto, int empresaId)
    {
        var proyecto = new Proyecto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            EmpresaId = empresaId,
            FechaCreacion = DateTime.UtcNow,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin
        };

        _context.Proyectos.Add(proyecto);
        await _context.SaveChangesAsync();

        return proyecto;
    }

    public async Task<List<Proyecto>> ObtenerProyectosPorEmpresaAsync(int empresaId)
    {
        return await _context.Proyectos
            .Where(p => p.EmpresaId == empresaId)
            .ToListAsync();
    }

    public async Task<bool> AsignarColaboradoresAsync(int empresaId, AsignarColaboradoresProyectoDto dto)
    {
            // Verifica que el proyecto pertenece a la empresa
        var proyecto = await _context.Proyectos
            .FirstOrDefaultAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

        if (proyecto == null) return false;

        // Filtra usuarios válidos de la misma empresa
        var usuarios = await _context.Usuarios
            .Where(u => dto.UsuarioIds.Contains(u.Id) && u.EmpresaId == empresaId && u.Rol == "colaborador")
            .ToListAsync();

        var nuevos = usuarios.Select(u => new ProyectoColaborador
        {
            ProyectoId = dto.ProyectoId,
            UsuarioId = u.Id
        });

        _context.ProyectoColaboradores.AddRange(nuevos);
        await _context.SaveChangesAsync();

        return true;
}


}
