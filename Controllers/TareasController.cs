using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;

namespace TaskTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin_empresa,superadmin")]
    public class TareasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TareaService _tareaService;
        private readonly IHttpContextAccessor _httpContext;

        public TareasController(AppDbContext context, TareaService tareaService, IHttpContextAccessor httpContext)
        {
            _context = context;
            _tareaService = tareaService;
            _httpContext = httpContext;
        }

        // 1. Crear nueva tarea
        [HttpPost]
        public async Task<ActionResult<Tarea>> CrearTarea(CreateTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tarea = await _tareaService.CrearTareaAsync(dto, empresaId.Value);
            if (tarea == null)
                return BadRequest("El proyecto no existe o no pertenece a tu empresa.");

            return CreatedAtAction(nameof(ObtenerDetalle), new { tareaId = tarea.Id }, tarea);
        }

        // 2. Obtener tareas de un proyecto
        [HttpGet("por-proyecto/{proyectoId}")]
        public async Task<ActionResult<List<Tarea>>> ObtenerTareasPorProyecto(int proyectoId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tareas = await _tareaService.ObtenerTareasPorProyectoAsync(proyectoId, empresaId.Value);
            return Ok(tareas);
        }

        // 3. Asignar colaboradores a una tarea
        [HttpPost("{tareaId}/asignar-colaboradores")]
        public async Task<IActionResult> AsignarColaboradores(int tareaId, [FromBody] List<int> usuarioIds)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.AsignarColaboradoresATareaAsync(tareaId, usuarioIds, empresaId.Value);
            if (!exito)
                return BadRequest("Tarea no encontrada o usuarios inválidos.");

            return Ok("Colaboradores asignados a la tarea.");
        }

        // 4. Cambiar estado de tarea
        [HttpPatch("{tareaId}/estado")]
        public async Task<IActionResult> CambiarEstadoTarea(int tareaId, [FromBody] CambiarEstadoTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.CambiarEstadoTareaAsync(tareaId, dto.NuevoEstado, empresaId.Value);
            if (!exito)
                return BadRequest("Tarea no encontrada o no pertenece a la empresa.");

            return Ok("Estado de tarea actualizado.");
        }

        // 5. Eliminar tarea
        [HttpDelete("{tareaId}")]
        public async Task<IActionResult> EliminarTarea(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.EliminarTareaAsync(tareaId, empresaId.Value);
            if (!exito)
                return NotFound("Tarea no encontrada o no pertenece a tu empresa.");

            return NoContent();
        }

        // 6. Obtener detalle completo de tarea (para el modal)
        [HttpGet("{tareaId}")]
        public async Task<IActionResult> ObtenerDetalle(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            if (tarea == null) return NotFound();

            var tareaDto = MapToTareaDetalleDto(tarea);
            return Ok(tareaDto);
        }

        // 7. Agregar comentario a tarea
        [HttpPost("{tareaId}/comentarios")]
        public async Task<IActionResult> AgregarComentario(int tareaId, [FromBody] ComentarioDto comentarioDto)
        {
            var empresaId = GetEmpresaIdFromToken();
            var usuarioId = GetUsuarioIdFromToken();
            if (empresaId == null || usuarioId == null) return Unauthorized();

            // Crear el comentario a partir del DTO
            var comentario = new TareaComentario
            {
                TareaId = tareaId,
                ComentarioTexto = comentarioDto.ComentarioTexto,
                UsuarioId = usuarioId.Value,
                FechaComentario = DateTime.UtcNow
            };

            // Guardar el comentario en la base de datos
            _context.TareaComentarios.Add(comentario);
            await _context.SaveChangesAsync();

            return Ok(comentario); // Puedes devolver el comentario completo si es necesario
        }

        // -----------------------
        // Métodos privados
        // -----------------------
        private int? GetEmpresaIdFromToken()
        {
            var empresaClaim = User.FindFirst("empresaId")?.Value;
            return int.TryParse(empresaClaim, out var id) ? id : null;
        }

        private int? GetUsuarioIdFromToken()
        {
            var usuarioClaim = User.FindFirst("usuarioId")?.Value;
            return int.TryParse(usuarioClaim, out var id) ? id : null;
        }

        private TareaDetalleDto MapToTareaDetalleDto(Tarea tarea)
        {
            return new TareaDetalleDto
            {
                Id = tarea.Id,
                Descripcion = tarea.Descripcion,
                Ubicacion = tarea.Ubicacion,
                FechaInicioEstimado = tarea.FechaInicioEstimado,
                FechaFinEstimado = tarea.FechaFinEstimado,
                Prioridad = tarea.Prioridad,
                Estado = tarea.Estado,
                Comentarios = tarea.Comentarios.Select(c => new ComentarioDto
                {
                    // Aquí accedemos al nombre del usuario desde la relación
                    UsuarioNombre = c.Usuario?.Name ?? "Desconocido", // Si no hay usuario, mostramos "Desconocido"
                    ComentarioTexto = c.ComentarioTexto,
                    FechaComentario = c.FechaComentario
                }).ToList(),
                SubTareas = tarea.SubTareas.Select(st => new SubTareaDto
                {
                    Id = st.Id,
                    Descripcion = st.Descripcion,
                    Completada = st.Completada
                }).ToList(),
                Adjuntos = tarea.Adjuntos.Select(a => new AdjuntoDto
                {
                    Id = a.Id,
                    ArchivoUrl = a.ArchivoUrl,
                    NombreArchivo = a.NombreArchivo,
                    FechaSubida = a.FechaSubida
                }).ToList()
            };
        }
    }
}
