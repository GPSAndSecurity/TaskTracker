using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;
using System.Security.Claims;

namespace TaskTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin_empresa,superadmin,colaborador")]
    public class TareasController : ControllerBase
    {
        private readonly TareaService _tareaService;

        public TareasController(TareaService tareaService)
        {
            _tareaService = tareaService;
        }

        [HttpGet("por-proyecto/{proyectoId}")]
        public async Task<IActionResult> ObtenerTareasPorProyecto(int proyectoId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tareas = await _tareaService.ObtenerTareasPorProyectoAsync(proyectoId, empresaId.Value);
            return Ok(tareas);
        }
        [HttpGet("{tareaId}")]
        public async Task<IActionResult> ObtenerDetalleTarea(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            if (tarea == null) return NotFound();

            // Mapear comentarios a un nuevo DTO o anónimo
            var comentariosConNombre = tarea.Comentarios.Select(c => new
            {
                c.Id,
                c.TareaId,
                c.ComentarioTexto,
                c.FechaComentario,
                UsuarioNombre = c.Usuario != null ? $"{c.Usuario.Name} {c.Usuario.Lastname}" : "Desconocido"
            }).ToList();

            // Retornar tarea con comentarios mapeados
            var tareaDto = new
            {
                tarea.Id,
                tarea.Descripcion,
                tarea.Ubicacion,
                tarea.Estado,
                tarea.FechaInicioEstimado,
                tarea.FechaFinEstimado,
                Comentarios = comentariosConNombre
            };

            return Ok(tareaDto);
        }


        [HttpPost("{tareaId}/comentarios")]
        public async Task<IActionResult> AgregarComentario(int tareaId, [FromBody] ComentarioDto comentarioDto)
        {
            var empresaId = GetEmpresaIdFromToken();
            var usuarioId = GetUsuarioIdFromToken();
            if (empresaId == null || usuarioId == null) return Unauthorized();

            var resultado = await _tareaService.AgregarComentarioAsync(
                tareaId, usuarioId.Value, comentarioDto.ComentarioTexto, empresaId.Value
            );

            if (resultado is null)
                return BadRequest("No se pudo agregar el comentario.");

            var (comentario, usuarioNombre) = resultado.Value;

            Console.WriteLine($"[DEBUG] UsuarioId desde token: {usuarioId.Value}, UsuarioNombre: {usuarioNombre}");

            return Ok(new
            {
                comentario.Id,
                comentario.TareaId,
                comentario.ComentarioTexto,
                comentario.FechaComentario,
                UsuarioNombre = usuarioNombre
            });
        }

        [HttpPatch("{tareaId}/estado")]
        public async Task<IActionResult> CambiarEstadoTarea(int tareaId, [FromBody] EstadoTarea nuevoEstado)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.CambiarEstadoTareaAsync(tareaId, nuevoEstado, empresaId.Value);
            if (!exito) return BadRequest();
            return NoContent();
        }

        [HttpDelete("{tareaId}")]
        public async Task<IActionResult> EliminarTarea(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.EliminarTareaAsync(tareaId, empresaId.Value);
            if (!exito) return NotFound();
            return NoContent();
        }

        [HttpPost("{tareaId}/asignar")]
        public async Task<IActionResult> AsignarColaboradores(int tareaId, [FromBody] List<int> usuarioIds)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.AsignarColaboradoresATareaAsync(tareaId, usuarioIds, empresaId.Value);
            if (!exito) return BadRequest();
            return NoContent();
        }

        // --- Helpers para claims ---
        private int? GetEmpresaIdFromToken()
        {
            var empresaClaim = User.FindFirst("empresaId")?.Value;
            return int.TryParse(empresaClaim, out var id) ? id : null;
        }

        private int? GetUsuarioIdFromToken()
        {
            var usuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(usuarioClaim, out var id) ? id : null;
        }
    
    
    [HttpPost("{tareaId}/adjuntos")]
        public async Task<IActionResult> SubirAdjunto(int tareaId, IFormFile archivo)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            if (tarea == null) return NotFound();

            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo inválido");

            // Guardar archivo en carpeta local (o storage)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var archivoNombre = $"{Guid.NewGuid()}_{archivo.FileName}";
            var filePath = Path.Combine(uploadsFolder, archivoNombre);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Guardar registro en base de datos
            var adjunto = await _tareaService.AgregarAdjuntoAsync(tareaId, archivoNombre, archivo.FileName);

            return Ok(adjunto);
        }
    }
}
