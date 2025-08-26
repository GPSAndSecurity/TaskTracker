using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
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
            .Include(t => t.Comentarios)
                .ThenInclude(c => c.Usuario)
            .Include(t => t.Asignados)
                .ThenInclude(a => a.Usuario)
            .ToListAsync();
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

    public async Task<(TareaComentario comentario, string usuarioNombre)?> AgregarComentarioAsync(
       int tareaId, int usuarioId, string comentarioTexto, int empresaId)
    {
        // Verificar que el usuario pertenece a la empresa
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.EmpresaId == empresaId);

        if (usuario == null) return null;

        // Verificar que la tarea existe
        var tarea = await _context.Tareas
            .FirstOrDefaultAsync(t => t.Id == tareaId);

        if (tarea == null) return null;

        var comentario = new TareaComentario
        {
            TareaId = tareaId,
            UsuarioId = usuarioId,
            ComentarioTexto = comentarioTexto,
            FechaComentario = DateTime.UtcNow
        };

        _context.TareaComentarios.Add(comentario);
        await _context.SaveChangesAsync();

        var usuarioNombre = $"{usuario.Name} {usuario.Lastname}";

        return (comentario, usuarioNombre);
    }

    public async Task<TareaAdjunto> AgregarAdjuntoAsync(int tareaId, string archivoUrl, string nombreArchivo)
    {
        var adjunto = new TareaAdjunto
        {
            TareaId = tareaId,
            ArchivoUrl = archivoUrl, // No le agregues "/Uploads/"
            NombreArchivo = nombreArchivo,
            FechaSubida = DateTime.UtcNow
        };

        _context.TareaAdjuntos.Add(adjunto);
        await _context.SaveChangesAsync();

        return adjunto;
    }

    public async Task ActualizarTareaAsync(Tarea tarea)
    {
        _context.Tareas.Update(tarea);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UsuarioDto>> ObtenerColaboradoresPorTareaAsync(int tareaId, int empresaId)
    {
        // Verificar que la tarea exista y pertenezca a la empresa
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return new List<UsuarioDto>();

        // Obtener usuarios asignados a la tarea
        var colaboradores = await _context.TareaAsignados
            .Where(ta => ta.TareaId == tareaId)
            .Include(ta => ta.Usuario)
            .Select(ta => ta.Usuario)
            .Where(u => u.EmpresaId == empresaId)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Name = u.Name,
                Lastname = u.Lastname,
                Email = u.Email
            })
            .ToListAsync();

        return colaboradores;
    }
    public async Task<List<Tarea>> ObtenerTareasAsignadasAColaboradorAsync(int proyectoId, int usuarioId, int empresaId)
    {
        return await _context.Tareas
            .Where(t => t.ProyectoId == proyectoId &&
                        t.Proyecto.EmpresaId == empresaId &&
                        t.Asignados.Any(a => a.UsuarioId == usuarioId))
            .Include(t => t.SubTareas)
            .Include(t => t.Adjuntos)
            .Include(t => t.Comentarios)
                .ThenInclude(c => c.Usuario)
            .ToListAsync();
    }

    public async Task<bool> EliminarColaboradorDeTareaAsync(int tareaId, int usuarioId, int empresaId)
    {
        // Verificar que la tarea exista y pertenezca a la empresa
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return false;

        // Buscar la asignación del colaborador en la tarea
        var asignacion = await _context.TareaAsignados
            .FirstOrDefaultAsync(ta => ta.TareaId == tareaId && ta.UsuarioId == usuarioId);

        if (asignacion == null)
            return false;

        _context.TareaAsignados.Remove(asignacion);
        await _context.SaveChangesAsync();

        return true;
    }
public async Task<string?> ObtenerNombreEmpresaPorId(int empresaId)
{
    var empresa = await _context.Empresas.FindAsync(empresaId);
    return empresa?.Nombre;
}

public async Task<string?> ObtenerNombreProyectoPorId(int proyectoId)
{
    var proyecto = await _context.Proyectos.FindAsync(proyectoId);
    return proyecto?.Nombre;
}

public async Task ComprimirYGuardarImagenAsync(Stream inputStream, string outputPath, string extension)
{
    using var image = await Image.LoadAsync(inputStream);

    if (extension is ".jpg" or ".jpeg")
    {
        var encoder = new JpegEncoder
        {
            Quality = 75 // Compresión al 75% de calidad
        };
        await image.SaveAsync(outputPath, encoder);
    }
    else if (extension == ".png")
    {
        var encoder = new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestCompression
        };
        await image.SaveAsync(outputPath, encoder);
    }
}
public async Task<List<ComentarioAdjuntoDto>> ObtenerComentariosYAdjuntosComoComentariosAsync(int tareaId, int empresaId)
{
    var tarea = await _context.Tareas
        .Include(t => t.Comentarios)
            .ThenInclude(c => c.Usuario)
        .Include(t => t.Adjuntos)
        .Include(t => t.Proyecto)
        .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto!.EmpresaId == empresaId);

    if (tarea == null)
        return new List<ComentarioAdjuntoDto>();

    var comentariosDtos = tarea.Comentarios.Select(c => new ComentarioAdjuntoDto
    {
        EsAdjunto = false,
        UsuarioNombre = $"{c.Usuario?.Name} {c.Usuario?.Lastname}",
        ComentarioTexto = c.ComentarioTexto,
        FechaComentario = c.FechaComentario
    });

    var adjuntosDtos = tarea.Adjuntos.Select(a => new ComentarioAdjuntoDto
    {
        EsAdjunto = true,
        AdjuntoId = a.Id,
        ArchivoUrl = a.ArchivoUrl,
        NombreArchivo = a.NombreArchivo,
        FechaComentario = a.FechaSubida
    });

    // Mezclamos y ordenamos por fecha
    var mezclado = comentariosDtos.Concat(adjuntosDtos)
        .OrderBy(ca => ca.FechaComentario)
        .ToList();

    return mezclado;
}

}

