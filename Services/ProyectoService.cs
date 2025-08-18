using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public class ProyectoService
    {
        private readonly AppDbContext _context;

        public ProyectoService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Crear proyecto
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

        // Obtener proyectos por empresa
        public async Task<List<Proyecto>> ObtenerProyectosPorEmpresaAsync(int empresaId)
        {
            return await _context.Proyectos
                                 .Where(p => p.EmpresaId == empresaId)
                                 .ToListAsync();
        }

        // Eliminar proyecto
        public async Task<bool> EliminarProyectoAsync(int proyectoId, int empresaId)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.Id == proyectoId && p.EmpresaId == empresaId);

            if (proyecto == null)
                return false;

            _context.Proyectos.Remove(proyecto);
            await _context.SaveChangesAsync();
            return true;
        }

        // Asignar colaboradores
public async Task<bool> AsignarColaboradoresAsync(int empresaId, AsignarColaboradoresProyectoDto dto)
{
    // Verificar que el proyecto exista y pertenezca a la empresa
    var proyecto = await _context.Proyectos
        .FirstOrDefaultAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

    if (proyecto == null)
        return false;

    // Obtener los usuarios válidos (colaboradores de la misma empresa)
    var usuarios = await _context.Usuarios
        .Where(u => dto.UsuarioIds.Contains(u.Id) && u.EmpresaId == empresaId && u.Rol == "colaborador")
        .ToListAsync();

    // Obtener colaboradores ya asignados a este proyecto
    var yaAsignados = await _context.ProyectoColaboradores
        .Where(pc => pc.ProyectoId == dto.ProyectoId && dto.UsuarioIds.Contains(pc.UsuarioId))
        .Select(pc => pc.UsuarioId)
        .ToListAsync();

    // Filtrar solo los nuevos colaboradores que aún no están asignados
    var nuevos = usuarios
        .Where(u => !yaAsignados.Contains(u.Id))
        .Select(u => new ProyectoColaborador
        {
            ProyectoId = dto.ProyectoId,
            UsuarioId = u.Id
        })
        .ToList();

    // Agregar solo si hay nuevos
    if (nuevos.Any())
    {
        _context.ProyectoColaboradores.AddRange(nuevos);
        await _context.SaveChangesAsync();
    }

    return true;
}

        // Obtener el avance del proyecto 
        public async Task<List<ProyectoConAvanceDto>> ObtenerProyectosConAvanceAsync(int empresaId)
        {
            var proyectos = await _context.Proyectos
                .Where(p => p.EmpresaId == empresaId)
                .Include(p => p.Tareas)
                .ToListAsync();

        return proyectos.Select(p =>
        {
            double porcentaje = 0;
            if (p.Tareas.Any())
            {
                var totalTareas = p.Tareas.Count;
                var tareasFinalizadas = p.Tareas.Count(t => t.Estado == EstadoTarea.Finalizada);
                porcentaje = (double)tareasFinalizadas / totalTareas * 100;
            }

            return new ProyectoConAvanceDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                PorcentajeAvance = Math.Round(porcentaje, 2)
            };
        }).ToList();

        }
        
        //Obtener proyectos que pertenencen a x colaborador, que se muestre solo 3 estados 
public async Task<List<ProyectoConTareasDto>> ObtenerProyectosAsignadosAColaboradorAsync(int usuarioId)
{
    var estadosValidos = new[] { EstadoTarea.EnProceso, EstadoTarea.Inconclusa, EstadoTarea.Finalizada };

    var proyectos = await _context.Proyectos
        .Where(p => p.Colaboradores.Any(pc => pc.UsuarioId == usuarioId))
        .Include(p => p.Tareas)
        .Where(p => p.Tareas.Any(t => estadosValidos.Contains(t.Estado)))
        .Select(p => new ProyectoConTareasDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            FechaInicio = p.FechaInicio,
            FechaFin = p.FechaFin,
            Tareas = p.Tareas
                .Where(t => estadosValidos.Contains(t.Estado))
                .Select(t => new TareaDetalleDto
                {
                    Id = t.Id,
                    Descripcion = t.Descripcion,
                    Estado = t.Estado
                }).ToList()
        })
        .ToListAsync();

    return proyectos;
}


        // Contar proyectos por empresa
        public async Task<int> ContarProyectosPorEmpresaAsync(int empresaId)
        {
            return await _context.Proyectos
                                 .Where(p => p.EmpresaId == empresaId)
                                 .CountAsync();
        }
    }

    
}
