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

        // Asignar colaboradores a un proyecto
        public async Task<bool> AsignarColaboradoresAsync(int empresaId, AsignarColaboradoresProyectoDto dto)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

            if (proyecto == null)
                return false;

            var usuarios = await _context.Usuarios
                .Where(u => dto.UsuarioIds.Contains(u.Id) && u.EmpresaId == empresaId && u.Rol == "colaborador")
                .ToListAsync();

            var yaAsignados = await _context.ProyectoColaboradores
                .Where(pc => pc.ProyectoId == dto.ProyectoId && dto.UsuarioIds.Contains(pc.UsuarioId))
                .Select(pc => pc.UsuarioId)
                .ToListAsync();

            var nuevos = usuarios
                .Where(u => !yaAsignados.Contains(u.Id))
                .Select(u => new ProyectoColaborador
                {
                    ProyectoId = dto.ProyectoId,
                    UsuarioId = u.Id
                })
                .ToList();

            if (nuevos.Any())
            {
                _context.ProyectoColaboradores.AddRange(nuevos);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        // Obtener proyectos con avance (para admins/empresa)
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
                    PorcentajeAvance = Math.Round(porcentaje, 2),
                    Archivado = p.Archivado

                };
            }).ToList();
        }

        // Obtener proyectos asignados a un colaborador con tareas en estados específicos
        public async Task<List<ProyectoConTareasDto>> ObtenerProyectosAsignadosAColaboradorAsync(int usuarioId)
        {
            var estadosValidos = new[] { EstadoTarea.EnProceso, EstadoTarea.Inconclusa, EstadoTarea.Finalizada };

            var proyectos = await _context.Proyectos
                .Where(p => p.Colaboradores.Any(pc => pc.UsuarioId == usuarioId))
                .Include(p => p.Tareas.Where(t => estadosValidos.Contains(t.Estado)))
                    .ThenInclude(t => t.Comentarios)
                        .ThenInclude(c => c.Usuario)
                .Include(p => p.Tareas.Where(t => estadosValidos.Contains(t.Estado)))
                    .ThenInclude(t => t.SubTareas)
                .Include(p => p.Tareas.Where(t => estadosValidos.Contains(t.Estado)))
                    .ThenInclude(t => t.Adjuntos)
                .ToListAsync();

            var resultado = proyectos.Select(p => new ProyectoConTareasDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                Archivado = p.Archivado,  // <--- ¡Aquí lo agregas!
                Tareas = p.Tareas
                    .Where(t => estadosValidos.Contains(t.Estado))
                    .Select(t => new TareaDetalleDto
                    {
                        Id = t.Id,
                        Descripcion = t.Descripcion,
                        Ubicacion = t.Ubicacion,
                        FechaInicioEstimado = t.FechaInicioEstimado,
                        FechaFinEstimado = t.FechaFinEstimado,
                        Prioridad = t.Prioridad,
                        Estado = t.Estado,
                        Comentarios = t.Comentarios.Select(c => new ComentarioDto
                        {
                            UsuarioNombre = c.Usuario != null ? $"{c.Usuario.Name} {c.Usuario.Lastname}".Trim() : "Desconocido",
                            ComentarioTexto = c.ComentarioTexto,
                            FechaComentario = c.FechaComentario
                        }).ToList(),
                        SubTareas = t.SubTareas.Select(st => new SubTareaDto
                        {
                            Id = st.Id,
                            Descripcion = st.Descripcion,
                            Completada = st.Completada
                        }).ToList(),
                        Adjuntos = t.Adjuntos.Select(a => new AdjuntoDto
                        {
                            Id = a.Id,
                            NombreArchivo = a.NombreArchivo,
                            ArchivoUrl = a.ArchivoUrl,
                            FechaSubida = a.FechaSubida
                        }).ToList()
                    }).ToList()
            }).ToList();

            return resultado;
        }

        // Contar proyectos por empresa
        public async Task<int> ContarProyectosPorEmpresaAsync(int empresaId)
        {
            return await _context.Proyectos
                                 .Where(p => p.EmpresaId == empresaId&& !p.Archivado)
                                 .CountAsync();
        }

        public async Task<List<UsuarioDto>> ObtenerColaboradoresPorProyectoAsync(int proyectoId)
        {
            var colaboradores = await _context.ProyectoColaboradores
                .Where(pc => pc.ProyectoId == proyectoId)
                .Include(pc => pc.Usuario)
                .Select(pc => new UsuarioDto
                {
                    Id = pc.Usuario.Id,
                    Name = pc.Usuario.Name,
                    Lastname = pc.Usuario.Lastname,
                    Email = pc.Usuario.Email,
                    Rol = pc.Usuario.Rol
                }).ToListAsync();

            return colaboradores;
        }

        public async Task<List<UsuarioDto>> ObtenerColaboradoresDisponiblesParaProyectoAsync(int proyectoId, int empresaId)
        {
            var colaboradoresEmpresa = await _context.Usuarios
                .Where(u => u.EmpresaId == empresaId && u.Rol == "colaborador")
                .ToListAsync();

            var asignadosIds = await _context.ProyectoColaboradores
                .Where(pc => pc.ProyectoId == proyectoId)
                .Select(pc => pc.UsuarioId)
                .ToListAsync();

            var disponibles = colaboradoresEmpresa
                .Where(u => !asignadosIds.Contains(u.Id))
                .Select(u => new UsuarioDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Lastname = u.Lastname,
                    Email = u.Email,
                    Rol = u.Rol
                })
                .ToList();

            return disponibles;
        }

        public async Task<bool> EliminarColaboradorDeProyectoAsync(int proyectoId, int usuarioId, int empresaId)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.Id == proyectoId && p.EmpresaId == empresaId);

            if (proyecto == null)
                return false;

            var asignacion = await _context.ProyectoColaboradores
                .FirstOrDefaultAsync(pc => pc.ProyectoId == proyectoId && pc.UsuarioId == usuarioId);

            if (asignacion == null)
                return false;

            _context.ProyectoColaboradores.Remove(asignacion);
            await _context.SaveChangesAsync();

            return true;
        }

        // 🔹 Obtener proyectos con avance filtrados por colaborador
        public async Task<List<ProyectoConAvanceDto>> ObtenerProyectosConAvancePorColaboradorAsync(int usuarioId)
        {
            var proyectos = await _context.Proyectos
                .Where(p => p.Colaboradores.Any(pc => pc.UsuarioId == usuarioId))
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

        // 🔹 Contar proyectos asignados a un colaborador
        public async Task<int> ContarProyectosAsignadosAColaboradorAsync(int usuarioId)
        {
            return await _context.Proyectos
                .CountAsync(p => p.Colaboradores.Any(pc => pc.UsuarioId == usuarioId));
        }




        public async Task<bool> ArchivarProyectoAsync(int id)
        {
            var proyecto = await _context.Proyectos
                                .Include(p => p.Tareas)
                                .Include(p => p.Colaboradores)
                                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null)
                return false;

            // Si el proyecto está vacío, eliminarlo
            if (!proyecto.Tareas.Any() && !proyecto.Colaboradores.Any())
            {
                _context.Proyectos.Remove(proyecto);
                await _context.SaveChangesAsync();
                return true;
            }

            // Si ya está archivado, no hacer nada
            if (proyecto.Archivado)
                return false;

            // Archivar: marcar como no activo y archivado
            proyecto.Activo = false;
            proyecto.Archivado = true;
            await _context.SaveChangesAsync();

            return true;
        }


public async Task<bool> DesarchivarProyectoAsync(int proyectoId)
{
    var proyecto = await _context.Proyectos.FindAsync(proyectoId);
    if (proyecto == null)
        return false;

    proyecto.Archivado = false;
    await _context.SaveChangesAsync();
    return true;
}


    }

    
}