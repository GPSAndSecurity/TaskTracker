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

            var comentariosConNombre = tarea.Comentarios.Select(c => new
            {
                c.Id,
                c.TareaId,
                c.ComentarioTexto,
                c.FechaComentario,
                UsuarioNombre = c.Usuario != null ? $"{c.Usuario.Name} {c.Usuario.Lastname}" : "Desconocido"
            }).ToList();
            var colaboradores = await _tareaService.ObtenerColaboradoresPorTareaAsync(tareaId, empresaId.Value);

            // Retornar tarea con comentarios mapeados
            var tareaDto = new
            {
                tarea.Id,
                tarea.Descripcion,
                tarea.Ubicacion,
                tarea.Estado,
                tarea.FechaInicioEstimado,
                tarea.FechaFinEstimado,
                Comentarios = comentariosConNombre,
                Asignados = colaboradores
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
        [Authorize(Roles = "admin_empresa,superadmin")]
        public async Task<IActionResult> AsignarColaboradores(int tareaId, [FromBody] AsignarUsuariosTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var exito = await _tareaService.AsignarColaboradoresATareaAsync(tareaId, dto.usuarioIds, empresaId.Value);
            if (!exito) return BadRequest();
            return NoContent();
        }


        // helpers para claim
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



        [HttpGet("{tareaId}/colaboradores")]
        public async Task<ActionResult<List<UsuarioDto>>> ObtenerColaboradoresPorTarea(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var colaboradores = await _tareaService.ObtenerColaboradoresPorTareaAsync(tareaId, empresaId.Value);
            return Ok(colaboradores);
        }


        [HttpPost]
        [Authorize(Roles = "admin_empresa,superadmin")]
        public async Task<IActionResult> CrearTarea([FromBody] CreateTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tarea = await _tareaService.CrearTareaAsync(dto, empresaId.Value);
            if (tarea == null) return BadRequest("No se pudo crear la tarea.");

            return CreatedAtAction(nameof(ObtenerDetalleTarea), new { tareaId = tarea.Id }, tarea);
        }

        [HttpPut("{tareaId}")]
        [Authorize(Roles = "admin_empresa,superadmin")]
        public async Task<IActionResult> ActualizarTarea(int tareaId, [FromBody] UpdateTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            if (tarea == null) return NotFound("Tarea no encontrada.");

            tarea.Descripcion = dto.Descripcion;
            tarea.Ubicacion = dto.Ubicacion;
            tarea.FechaInicioEstimado = dto.FechaInicioEstimado;
            tarea.FechaFinEstimado = dto.FechaFinEstimado;
            tarea.Prioridad = dto.Prioridad;
            tarea.AttachmentRequerido = dto.AttachmentRequerido;
            tarea.UbicacionRequeridaAlCerrar = dto.UbicacionRequeridaAlCerrar;

            await _tareaService.ActualizarTareaAsync(tarea);

            return Ok(tarea);
        }
        [HttpGet("por-proyecto/{proyectoId}/asignadas")]
        [Authorize(Roles = "colaborador")]
        public async Task<IActionResult> ObtenerTareasAsignadasAlColaborador(int proyectoId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            var empresaId = GetEmpresaIdFromToken();

            if (usuarioId == null || empresaId == null)
                return Unauthorized();

            var tareas = await _tareaService.ObtenerTareasAsignadasAColaboradorAsync(proyectoId, usuarioId.Value, empresaId.Value);

            return Ok(tareas);
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
        

        [HttpDelete("{tareaId}/colaboradores/{usuarioId}")]
public async Task<IActionResult> EliminarColaboradorDeTarea(int tareaId, int usuarioId)
{
    var empresaId = GetEmpresaIdFromToken();
    if (empresaId == null) return Unauthorized();

    // Llamamos al servicio para eliminar el colaborador de la tarea
    var exito = await _tareaService.EliminarColaboradorDeTareaAsync(tareaId, usuarioId, empresaId.Value);

    if (!exito) return NotFound();

    return NoContent();
}

    }
    
    
}