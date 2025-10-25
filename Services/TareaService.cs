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
    private readonly UbicacionService _ubicacionService;

    public TareaService(AppDbContext context, UbicacionService ubicacionService)
    {
        _context = context;
        _ubicacionService = ubicacionService;
    }

    public async Task<Tarea?> CrearTareaAsync(CreateTareaDto dto, int empresaId)
{
    var proyecto = await _context.Proyectos
        .FirstOrDefaultAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

    if (proyecto == null) return null;

    // Validar ubicación si se proporciona
    if (dto.UbicacionId.HasValue)
    {
        var ubicacionValida = await _context.Ubicaciones
            .AnyAsync(u => u.Id == dto.UbicacionId.Value && u.EmpresaId == empresaId);

        if (!ubicacionValida)
            return null;
    }

    var tarea = new Tarea
    {
        ProyectoId = dto.ProyectoId,
        Descripcion = dto.Descripcion,
        UbicacionId = dto.UbicacionId,
        FechaInicioEstimado = dto.FechaInicioEstimado,
        FechaFinEstimado = dto.FechaFinEstimado,
        Prioridad = dto.Prioridad,
        AttachmentRequerido = dto.AttachmentRequerido,
        UbicacionRequeridaAlCerrar = dto.UbicacionRequeridaAlCerrar,
        Estado = EstadoTarea.Pendiente,
        Presupuesto = dto.Presupuesto  

    };

    _context.Tareas.Add(tarea);
    await _context.SaveChangesAsync(); // Guardar para obtener el ID de la tarea

    // Guardar Datos Técnicos si existen en el DTO
    if (dto.DatosTecnicos != null)
    {
        foreach (var dtDto in dto.DatosTecnicos)
        {
            var datosTecnicos = new DatosTecnicos
            {
                TareaId = tarea.Id,
                VehiculoMarca = dtDto.VehiculoMarca,
                VehiculoModelo = dtDto.VehiculoModelo,
                VehiculoTipo = dtDto.VehiculoTipo,
                VehiculoCodigo = dtDto.VehiculoCodigo,
                VehiculoPlaca = dtDto.VehiculoPlaca,
                VehiculoVin = dtDto.VehiculoVin,
                GpsSerie = dtDto.GpsSerie,
                GpsImei = dtDto.GpsImei,
                SIMCompania = dtDto.SimCompania,
                SIMCodigo = dtDto.SimCodigo,
                InstalacionAccesorios = dtDto.InstalacionAccesorios,
                TecnicoInstalador = dtDto.TecnicoInstalador,
                FirmaCliente = dtDto.FirmaCliente,
                TiposTrabajo = dtDto.TiposTrabajo?.Select(tipoStr => new TareaTipoTrabajo
                {
                    TipoTrabajo = Enum.Parse<TipoTrabajo>(tipoStr, true)
                }).ToList() ?? new List<TareaTipoTrabajo>()
            };

            _context.DatosTecnicos.Add(datosTecnicos);
        }

        await _context.SaveChangesAsync(); // Guardar Datos Técnicos
    }

    return tarea;
}



    public async Task<List<Tarea>> ObtenerTareasPorProyectoAsync(int proyectoId, int empresaId, bool? soloArchivadas = null)
    {
        var proyectoValido = await _context.Proyectos
            .AnyAsync(p => p.Id == proyectoId && p.EmpresaId == empresaId);
        if (!proyectoValido)
            return new List<Tarea>();

        var query = _context.Tareas
            .Where(t => t.ProyectoId == proyectoId);

        if (soloArchivadas.HasValue)
        {
            if (soloArchivadas.Value)
                query = query.Where(t => t.Estado == EstadoTarea.Archivada);
            else
                query = query.Where(t => t.Estado != EstadoTarea.Archivada);
        }

        // Aquí encadenamos todos los Include
        var tareas = await query
            .Include(t => t.Ubicacion)
            .Include(t => t.Comentarios)
                .ThenInclude(c => c.Usuario)
            .Include(t => t.Asignados)
                .ThenInclude(a => a.Usuario)
            .Include(t => t.SubTareas)  
            .Include(t => t.Adjuntos)  
            .ToListAsync();

        return tareas;
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
            .Include(t => t.Cliente)
            .Include(t => t.Ubicacion)
            .Include(t => t.DatosTecnicos)
                .ThenInclude(dt => dt.TiposTrabajo) 
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto!.EmpresaId == empresaId);

        return tarea;
    }

    public async Task<bool> AsignarClienteATareaAsync(int tareaId, int? clienteId, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return false;

        if (clienteId.HasValue)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == clienteId.Value && c.EmpresaId == empresaId);

            if (cliente == null)
                return false;

            tarea.ClienteId = clienteId.Value;
        }
        else
        {
            tarea.ClienteId = null; 
        }

        await _context.SaveChangesAsync();
        return true;
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


        var yaAsignados = await _context.TareaAsignados
            .Where(ta => ta.TareaId == tareaId && usuarioIds.Contains(ta.UsuarioId))
            .Select(ta => ta.UsuarioId)
            .ToListAsync();


        var nuevosAsignados = usuarios
            .Where(u => !yaAsignados.Contains(u.Id))
            .Select(u => new TareaAsignado
            {
                TareaId = tareaId,
                UsuarioId = u.Id
            });


        _context.TareaAsignados.AddRange(nuevosAsignados);
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

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.EmpresaId == empresaId);

        if (usuario == null) return null;


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
            ArchivoUrl = archivoUrl,
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
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return new List<UsuarioDto>();


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

        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return false;

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
                Quality = 75
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

        var mezclado = comentariosDtos.Concat(adjuntosDtos)
            .OrderBy(ca => ca.FechaComentario)
            .ToList();

        return mezclado;
    }

    public async Task<bool> ArchivarTareaAsync(int tareaId)
    {
        var tarea = await _context.Tareas.FindAsync(tareaId);
        if (tarea == null || tarea.Estado == EstadoTarea.Archivada)
            return false;

        tarea.Estado = EstadoTarea.Archivada;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DesarchivarTareaAsync(int tareaId)
    {
        var tarea = await _context.Tareas.FindAsync(tareaId);
        if (tarea == null || tarea.Estado != EstadoTarea.Archivada)
            return false;
        tarea.Estado = EstadoTarea.Pendiente;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProyectoConTareasDto>> ObtenerProyectosAsignadosAUsuarioAsync(int usuarioId, int empresaId)
    {
        var proyectos = await _context.Proyectos
            .Where(p => p.EmpresaId == empresaId &&
                        p.Tareas.Any(t => t.Asignados.Any(a => a.UsuarioId == usuarioId)))
            .Select(p => new ProyectoConTareasDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                Archivado = p.Archivado,

                Tareas = p.Tareas
                    .Where(t => t.Asignados.Any(a => a.UsuarioId == usuarioId))
                    .Select(t => new TareaDetalleDto
                    {
                        Descripcion = t.Descripcion,
                        FechaInicioEstimado = t.FechaInicioEstimado,
                        FechaFinEstimado = t.FechaFinEstimado,
                        Estado = t.Estado
                    }).ToList()
            })
            .ToListAsync();

        return proyectos;
    }


    public async Task<List<Tarea>> ObtenerTareasArchivadasPorProyectoAsync(int proyectoId, int empresaId)
    {
        return await _context.Tareas
            .Where(t => t.ProyectoId == proyectoId &&
                        t.Proyecto.EmpresaId == empresaId &&
                        t.Estado == EstadoTarea.Archivada)
            .ToListAsync();
    }


    public async Task<List<Tarea>> ObtenerTareasArchivadasAsync(int empresaId)
    {
        return await _context.Tareas
            .Include(t => t.Proyecto)
            .Where(t => t.Estado == EstadoTarea.Archivada && t.Proyecto!.EmpresaId == empresaId)
            .ToListAsync();
    }


    public async Task<List<SubTarea>> ObtenerSubTareasPorTareaAsync(int tareaId, int empresaId)
    {
        return await _context.SubTareas
            .Where(st => st.TareaId == tareaId && st.Tarea.Proyecto.EmpresaId == empresaId)
            .ToListAsync();
    }
    public async Task<bool> ActualizarEstadoSubtareaAsync(int tareaId, int subtareaId, bool completada)
    {
        var subtarea = await _context.SubTareas
            .FirstOrDefaultAsync(st => st.Id == subtareaId && st.TareaId == tareaId);

        if (subtarea == null)
            return false;

        subtarea.Completada = completada;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> VerificarSubtareaExisteAsync(int tareaId, int subtareaId, int empresaId)
    {
        return await _context.SubTareas
            .AnyAsync(st => st.Id == subtareaId &&
                            st.TareaId == tareaId &&
                            st.Tarea.Proyecto.EmpresaId == empresaId);
    }

    public async Task<bool> AsignarClienteATareaAsync(AsignarClienteTareaDto dto, int empresaId)
    {
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == dto.TareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return false;

        if (dto.ClienteId.HasValue)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value && c.EmpresaId == empresaId);

            if (cliente == null)
                return false;

            tarea.ClienteId = cliente.Id;
        }
        else
        {
            tarea.ClienteId = null; 
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SubTarea?> CrearSubtareaAsync(int tareaId, CreateSubtareaDto dto, int empresaId)
    {
        // Validar que la tarea exista y pertenezca a la empresa
        var tarea = await _context.Tareas
            .Include(t => t.Proyecto)
            .FirstOrDefaultAsync(t => t.Id == tareaId && t.Proyecto.EmpresaId == empresaId);

        if (tarea == null)
            return null;

        var subtarea = new SubTarea
        {
            TareaId = tareaId,
            Descripcion = dto.Descripcion,
            Completada = dto.Completada
        };

        _context.SubTareas.Add(subtarea);
        await _context.SaveChangesAsync();

        return subtarea;
    }
public async Task<bool> EliminarSubtareaAsync(int tareaId, int subtareaId)
{
    var subtarea = await _context.SubTareas
        .FirstOrDefaultAsync(s => s.Id == subtareaId && s.TareaId == tareaId);

    if (subtarea == null)
        return false;

    _context.SubTareas.Remove(subtarea);
    await _context.SaveChangesAsync();
    return true;
}

    public async Task<List<Tarea>> ObtenerTodasLasTareasAsync(int empresaId)
    {
        return await _context.Tareas
            .Include(t => t.Proyecto)
            .Include(t => t.Comentarios)
                .ThenInclude(c => c.Usuario)
            .Include(t => t.Asignados)
                .ThenInclude(a => a.Usuario)
            .Include(t => t.Adjuntos)
            .Include(t => t.SubTareas)
            .Where(t => t.Proyecto.EmpresaId == empresaId)
            .ToListAsync();
    }

public async Task AgregarDatosTecnicosAsync(DatosTecnicos datosTecnicos)
{
    _context.DatosTecnicos.Add(datosTecnicos);
    await _context.SaveChangesAsync();
}

public async Task EliminarDatosTecnicosPorTareaAsync(int tareaId)
{
    var existentes = await _context.DatosTecnicos
        .Where(dt => dt.TareaId == tareaId)
        .ToListAsync();

    _context.DatosTecnicos.RemoveRange(existentes);
    await _context.SaveChangesAsync();
}

    public async Task<List<TareasPorClienteDto>> ObtenerTareasAgrupadasPorClienteAsync(int empresaId)
    {
        var query = _context.TareaAsignados
            .Include(ta => ta.Tarea)
            .ThenInclude(t => t.Cliente)
            .Include(ta => ta.Usuario)
            .Where(ta => ta.Usuario.EmpresaId.HasValue && ta.Usuario.EmpresaId.Value == empresaId
                && ta.Tarea.ClienteId.HasValue)
            .AsQueryable();

        var result = await query
            .GroupBy(ta => new
            {
                ClienteId = ta.Tarea.Cliente!.Id,
                ClienteNombre = ta.Tarea.Cliente.Nombre
            })
            .Select(g => new TareasPorClienteDto
            {
                ClienteId = g.Key.ClienteId,
                ClienteNombre = g.Key.ClienteNombre,
                EnProceso = g.Count(ta => ta.Tarea.Estado == EstadoTarea.EnProceso),
                Finalizadas = g.Count(ta => ta.Tarea.Estado == EstadoTarea.Finalizada),
                Inconclusas = g.Count(ta => ta.Tarea.Estado == EstadoTarea.Inconclusa),
                Total = g.Count()
            })
            .ToListAsync();

        return result;
    }
    

    
}
