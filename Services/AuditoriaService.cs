using TaskTracker.Models;
using TaskTracker.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


public class AuditoriaFilterDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Accion { get; set; }
    public int? UsuarioId { get; set; }
}

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

        public async Task RegistrarEventoAsync(string accion, string entidad, int? entidadId, string? descripcion)
        {
            var usuarioId = ObtenerUsuarioIdDesdeToken();

            var log = new Auditoria
            {
                UsuarioId = usuarioId,
                Accion = accion,
                Entidad = entidad,
                EntidadId = entidadId,
                Descripcion = descripcion,
                Fecha = DateTime.UtcNow
            };

            _context.Auditorias.Add(log);
            await _context.SaveChangesAsync();
        }

        private int? ObtenerUsuarioIdDesdeToken()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;

            return int.TryParse(claim.Value, out int userId) ? userId : null;
        }

        public async Task<List<Auditoria>> ObtenerLogsAsync(DateTime? fechaInicio, DateTime? fechaFin, string? accion, int? usuarioId)
        {
            var query = _context.Auditorias.AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(a => a.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(a => a.Fecha <= fechaFin.Value);

            if (!string.IsNullOrEmpty(accion))
                query = query.Where(a => a.Accion.Contains(accion));

            if (usuarioId.HasValue)
                query = query.Where(a => a.UsuarioId == usuarioId.Value);

            return await query
                .Include(a => a.Usuario) // Para traer datos del usuario que hizo la acción
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();
        }


    }
}
