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
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

            if (proyecto == null) return false;

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

        // ✅ Contar proyectos por empresa
        public async Task<int> ContarProyectosPorEmpresaAsync(int empresaId)
        {
            return await _context.Proyectos
                                 .Where(p => p.EmpresaId == empresaId)
                                 .CountAsync();
        }
    }
}
