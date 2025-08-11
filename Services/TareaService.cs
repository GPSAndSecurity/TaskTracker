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
            Estado = EstadoTarea.Pendiente
        };

        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();

        return tarea;
    }

    public async Task<List<Tarea>> ObtenerTareasPorProyectoAsync(int proyectoId, int empresaId)
    {
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

    public async Task<Tarea?> ObtenerTareaDetalleAsync(int tareaId, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Comentarios)
                .ThenInclude(c => c.Usuario)
            .Include(t => t.Asignados)
                .ThenInclude(a => a.Usuario)
            .Include(t => t.Adjuntos)
            .Include(t => t.SubTareas)
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto!.EmpresaId == empresaId);

        return tarea;
    }

    public async Task<TareaComentario?> AgregarComentarioAsync(int tareaId, int usuarioId, string texto, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto!.EmpresaId == empresaId);

        if (tarea == null) return null;

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId && u.EmpresaId == empresaId);
        if (usuario == null) return null;

        var comentario = new TareaComentario
        {
            TareaId = tareaId,
            UsuarioId = usuarioId,
            ComentarioTexto = texto,
            FechaComentario = DateTime.UtcNow
        };

        _context.TareaComentarios.Add(comentario);
        await _context.SaveChangesAsync();

        return comentario;
    }
}
