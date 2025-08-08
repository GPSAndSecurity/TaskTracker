using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Services;

public class TareaService
{
    private readonly AppDbContext _context;

    public TareaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tarea?> CrearTareaAsync(CreateTareaDto dto, int empresaId)
    {
        // Verificar que el proyecto pertenezca a la empresa
        var proyecto = await _context.Proyectos
            .FirstOrDefaultAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

        if (proyecto == null) return null;

        var tarea = new Tarea
        {
            ProyectoId = dto.ProyectoId,
            Descripcion = dto.Descripcion,
            Ubicacion = dto.Ubicacion,
            FechaInicioEstimado = dto.FechaInicioEstimado,
            FechaFinEstimado = dto.FechaFinEstimado,
            Prioridad = dto.Prioridad,
            AttachmentRequerido = dto.AttachmentRequerido,
            UbicacionRequeridaAlCerrar = dto.UbicacionRequeridaAlCerrar,
            Estado = EstadoTarea.Pendiente // Asegúrate que esté definido en tu enum
        };

        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();

        return tarea;
    }

    public async Task<List<Tarea>> ObtenerTareasPorProyectoAsync(int proyectoId, int empresaId)
    {
        // Validar que el proyecto pertenezca a la empresa
        var proyectoValido = await _context.Proyectos
            .AnyAsync(p => p.Id == proyectoId && p.EmpresaId == empresaId);
        if (!proyectoValido)
            return new List<Tarea>();

        return await _context.Tareas
            .Where(t => t.ProyectoId == proyectoId)
            .ToListAsync();
    }

    public async Task<bool> AsignarColaboradoresATareaAsync(int tareaId, List<int> usuarioIds, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId);

        if (tarea == null || tarea.Proyecto == null || tarea.Proyecto.EmpresaId != empresaId)
            return false;

        var usuarios = await _context.Usuarios
            .Where(u => usuarioIds.Contains(u.Id) && u.EmpresaId == empresaId)
            .ToListAsync();

        var asignaciones = usuarios.Select(u => new TareaAsignado
        {
            TareaId = tareaId,
            UsuarioId = u.Id
        });

        _context.TareaAsignados.AddRange(asignaciones);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CambiarEstadoTareaAsync(int tareaId, EstadoTarea nuevoEstado, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId);

        if (tarea == null || tarea.Proyecto == null || tarea.Proyecto.EmpresaId != empresaId)
            return false;

        tarea.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarTareaAsync(int tareaId, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return false;

        _context.Tareas.Remove(tarea);
        await _context.SaveChangesAsync();
        return true;
    }
}
