using TaskTracker.Models;
using TaskTracker.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;



namespace TaskTracker.Services
{
    public class AuditoriaService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? ObtenerUsuarioIdDesdeToken()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var userId) ? userId : null;
        }

        public async Task RegistrarEventoAsync(string accion, string entidad, int? entidadId, string? descripcion, bool generaNotificacion = false)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            var log = new Auditoria
            {
                UsuarioId = usuarioId,
                UsuarioGeneradorId = usuarioId, 
                Accion = accion,
                Entidad = entidad,
                EntidadId = entidadId,
                Descripcion = descripcion,
                Fecha = DateTime.UtcNow,
                GeneraNotificacion = generaNotificacion,
                Visto = false
            };

            _context.Auditorias.Add(log);
            await _context.SaveChangesAsync();
        }

      public async Task<List<Auditoria>> ObtenerEventosAuditoriaAsync(EventoAuditoriaFilterDto filtros)
{
    var usuarioActualId = ObtenerUsuarioIdDesdeToken();

    var query = _context.Auditorias
        .Include(a => a.Usuario)
        .AsQueryable();

    // Aplica filtros básicos
    if (filtros.FechaInicio.HasValue)
        query = query.Where(a => a.Fecha >= filtros.FechaInicio.Value);

    if (filtros.FechaFin.HasValue)
        query = query.Where(a => a.Fecha <= filtros.FechaFin.Value);

    if (!string.IsNullOrEmpty(filtros.Accion))
        query = query.Where(a => a.Accion.Contains(filtros.Accion));

    if (filtros.SoloNotificaciones == true)
        query = query.Where(a => a.GeneraNotificacion == true);

    if (filtros.SoloHistorial == true)
        query = query.Where(a => a.GeneraNotificacion == false);

    if (filtros.NoVistas == true)
        query = query.Where(a => a.Visto == false);

    // Aquí filtras según el usuario y su rol
    if (usuarioActualId.HasValue)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioActualId.Value);

        bool esAdmin = usuario != null && (usuario.Rol == "admin_empresa" || usuario.Rol == "superadmin");

        if (esAdmin)
        {
            // Admin: mostrar todas excepto las propias
            query = query.Where(a =>
                a.UsuarioId == usuarioActualId.Value &&
                a.UsuarioGeneradorId != usuarioActualId.Value);        }
        else
        {
            // Usuario normal: solo sus propias notificaciones
            query = query.Where(a => a.UsuarioId == usuarioActualId.Value);
        }
    }
    else if (filtros.UsuarioId.HasValue)
    {
        // Si se especifica un usuario explícitamente en filtros
        query = query.Where(a => a.UsuarioId == filtros.UsuarioId.Value);
    }

    return await query.OrderByDescending(a => a.Fecha).ToListAsync();
}

        public async Task<Auditoria?> MarcarComoVistaAsync(int id)
        {
            var notificacion = await _context.Auditorias.FirstOrDefaultAsync(a => a.Id == id);
            if (notificacion == null)
                return null;

            notificacion.Visto = true;
            await _context.SaveChangesAsync();
            return notificacion;
        }

        public async Task NotificarUsuariosRelacionadosConTareaAsync(
            int tareaId,
            int usuarioQueRealizoLaAccion,
            string accion,
            string descripcion,
            string entidad,
            int entidadId)
        {
            var ahora = DateTime.UtcNow;

            var empresaId = await _context.Usuarios
                .Where(u => u.Id == usuarioQueRealizoLaAccion)
                .Select(u => u.EmpresaId)
                .FirstOrDefaultAsync();

            var colaboradores = await _context.TareaAsignados
                .Where(a => a.TareaId == tareaId && a.UsuarioId != usuarioQueRealizoLaAccion)
                .Select(a => a.UsuarioId)
                .ToListAsync();

            var administradores = await _context.Usuarios
                .Where(u =>
                    u.Id != usuarioQueRealizoLaAccion &&
                    (u.Rol == "admin_empresa" || u.Rol == "superadmin") &&
                    (empresaId == null || u.EmpresaId == empresaId))
                .Select(u => u.Id)
                .ToListAsync();

            var destinatarios = colaboradores
                .Concat(administradores)
                .Distinct()
                .Where(id => id != usuarioQueRealizoLaAccion)
                .ToHashSet();

            foreach (var userId in destinatarios)
            {
                var notificacion = new Auditoria
                {
                    UsuarioId = userId,
                     UsuarioGeneradorId = usuarioQueRealizoLaAccion, 
                    Accion = accion,
                    Descripcion = descripcion,
                    Entidad = entidad,
                    EntidadId = entidadId,
                    Fecha = ahora,
                    Visto = false,
                    GeneraNotificacion = true
                };

                _context.Auditorias.Add(notificacion);
            }

            await _context.SaveChangesAsync();
        }
    }
}