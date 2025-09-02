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
        private readonly AuditoriaService _auditoriaService;

        public TareasController(TareaService tareaService, AuditoriaService auditoriaService)
        {
            _tareaService = tareaService;
            _auditoriaService = auditoriaService;
        }
        //obtener todas las tareas
[HttpGet]
[Authorize(Roles = "admin_empresa,superadmin")]
public async Task<IActionResult> ObtenerTodasLasTareas()
{
    var empresaId = GetEmpresaIdFromToken();
    if (empresaId == null) return Unauthorized();

    var tareas = await _tareaService.ObtenerTodasLasTareasAsync(empresaId.Value);
    return Ok(tareas);
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
                Asignados = colaboradores,
                SubTareas = tarea.SubTareas.Select(st => new SubTareaDto
                {
                    Id = st.Id,
                    Descripcion = st.Descripcion,
                    Completada = st.Completada
                }).ToList()
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

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Agregar Comentario",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: $"Usuario '{usuarioNombre}' agregó un comentario a la tarea ID {tareaId}",
                 generaNotificacion: true
            );

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

    var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
    if (tarea == null) return NotFound("Tarea no encontrada.");

    var estadoAnterior = tarea.Estado;

    // ✅ Lógica para evitar marcar como finalizada si hay subtareas pendientes
    if (nuevoEstado == EstadoTarea.Finalizada)
    {
        var subtareas = await _tareaService.ObtenerSubTareasPorTareaAsync(tareaId, empresaId.Value);
        bool haySubtareasPendientes = subtareas.Any(st => !st.Completada);

        if (haySubtareasPendientes)
        {
            return BadRequest("No se puede finalizar la tarea porque tiene subtareas pendientes.");
        }
    }

    var exito = await _tareaService.CambiarEstadoTareaAsync(tareaId, nuevoEstado, empresaId.Value);
    if (!exito) return BadRequest("No se pudo cambiar el estado.");

    await _auditoriaService.RegistrarEventoAsync(
        accion: "Cambiar Estado",
        entidad: "Tarea",
        entidadId: tareaId,
        descripcion: $"Se cambió el estado de la tarea '{tarea.Descripcion}' de '{estadoAnterior}' a '{nuevoEstado}'",
        generaNotificacion: true
    );

    return NoContent();
}


        [HttpDelete("{tareaId}")]
        public async Task<IActionResult> EliminarTarea(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            // Primero, obtener colaboradores asignados a esta tarea
            var colaboradores = await _tareaService.ObtenerColaboradoresPorTareaAsync(tareaId, empresaId.Value);

            // Verificar si la tarea tiene colaboradores
            if (colaboradores != null && colaboradores.Any())
            {
                return BadRequest("No se puede eliminar la tarea porque tiene colaboradores asignados.");
            }

            // Si no hay colaboradores, proceder a eliminar
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

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Asignar Colaboradores",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: $"Se asignaron usuarios [{string.Join(", ", dto.usuarioIds)}] a la tarea ID {tareaId}",
                 generaNotificacion: true
            );

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

        [HttpGet("{tareaId}/subtareas")]
        public async Task<IActionResult> ObtenerSubTareas(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var subtareas = await _tareaService.ObtenerSubTareasPorTareaAsync(tareaId, empresaId.Value);

            var subtareaDtos = subtareas.Select(st => new SubTareaDto
            {
                Id = st.Id,
                Descripcion = st.Descripcion,
                Completada = st.Completada
            }).ToList();

            return Ok(subtareaDtos);
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

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Crear Tarea",
                entidad: "Tarea",
                entidadId: tarea.Id,
                descripcion: $"Se creó la tarea '{tarea.Descripcion}'",
                 generaNotificacion: true
            );

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

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Editar Tarea",
                entidad: "Tarea",
                entidadId: tarea.Id,
                descripcion: $"Se actualizó la tarea '{tarea.Descripcion}'",
                 generaNotificacion: true
            );

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

            if (archivo.Length > 5 * 1024 * 1024) // 5MB
                return BadRequest("El archivo excede el tamaño máximo de 5MB.");

            // Obtener nombres legibles
            var empresaNombre = await _tareaService.ObtenerNombreEmpresaPorId(empresaId.Value); // Debes implementar este método
            var proyectoNombre = await _tareaService.ObtenerNombreProyectoPorId(tarea.ProyectoId); // Este también

            if (string.IsNullOrEmpty(empresaNombre) || string.IsNullOrEmpty(proyectoNombre))
                return BadRequest("No se pudo determinar empresa o proyecto.");

            // Sanitizar nombres de carpetas
            var empresaFolder = empresaNombre.Replace(" ", "_").Trim();
            var proyectoFolder = proyectoNombre.Replace(" ", "_").Trim();

            // Crear ruta organizada
            var basePath = Path.Combine("Uploads", empresaFolder, proyectoFolder);
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

            var extension = Path.GetExtension(archivo.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(basePath, fileName);

            // Comprimir imagen si es JPG o PNG
            if (extension is ".jpg" or ".jpeg" or ".png")
            {
                using var stream = archivo.OpenReadStream();
                await _tareaService.ComprimirYGuardarImagenAsync(stream, fullPath, extension);

            }
            else
            {
                using var stream = new FileStream(fullPath, FileMode.Create);
                await archivo.CopyToAsync(stream);
            }

            // Guardar registro en la BD
            var url = $"/Uploads/{empresaFolder}/{proyectoFolder}/{fileName}";
            var adjunto = await _tareaService.AgregarAdjuntoAsync(tareaId, url, archivo.FileName);

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

        [HttpGet("{tareaId}/comentarios-adjuntos")]
        public async Task<IActionResult> ObtenerComentariosYAdjuntos(int tareaId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var lista = await _tareaService.ObtenerComentariosYAdjuntosComoComentariosAsync(tareaId, empresaId.Value);

            if (lista == null || lista.Count == 0)
                return NotFound("No se encontraron comentarios ni adjuntos.");

            return Ok(lista);
        }

        [HttpPut("{tareaId}/archivar")]
        [Authorize(Roles = "superadmin,admin_empresa")]
        public async Task<IActionResult> ArchivarTarea(int tareaId)
        {
            var resultado = await _tareaService.ArchivarTareaAsync(tareaId);
            if (!resultado)
                return BadRequest("Tarea no encontrada, ya archivada o no se puede archivar.");

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Archivar Tarea",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: $"Se archivó la tarea con ID {tareaId}",
                 generaNotificacion: true
            );

            return Ok("Tarea archivada correctamente.");
        }


        [HttpPut("{tareaId}/desarchivar")]
        [Authorize(Roles = "superadmin,admin_empresa")]
        public async Task<IActionResult> DesarchivarTarea(int tareaId)
        {
            var resultado = await _tareaService.DesarchivarTareaAsync(tareaId);
            if (!resultado)
                return BadRequest("No se pudo desarchivar la tarea.");

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Desarchivar Tarea",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: $"Se desarchivó la tarea con ID {tareaId}",
                 generaNotificacion: true
            );

            return Ok("Tarea desarchivada correctamente.");
        }

        [HttpGet("proyectos-con-tareas-asignadas")]
        [Authorize(Roles = "colaborador")]
        public async Task<IActionResult> ObtenerProyectosConTareasAsignadas()
        {
            var usuarioId = GetUsuarioIdFromToken();
            var empresaId = GetEmpresaIdFromToken();
            if (usuarioId == null || empresaId == null) return Unauthorized();

            var proyectos = await _tareaService.ObtenerProyectosAsignadosAUsuarioAsync(usuarioId.Value, empresaId.Value);
            if (proyectos == null || !proyectos.Any()) return Ok(new List<object>());

            var resultado = new List<object>();

            foreach (var proyecto in proyectos)
            {
                var tareasAsignadas = await _tareaService.ObtenerTareasAsignadasAColaboradorAsync(proyecto.Id, usuarioId.Value, empresaId.Value);

                var proyectoDto = new
                {
                    proyecto.Id,
                    proyecto.Nombre,
                    proyecto.Descripcion,
                    proyecto.FechaInicio,
                    proyecto.FechaFin
                };

                var tareasDto = tareasAsignadas.Select(t => new
                {
                    t.Descripcion,
                    t.FechaInicioEstimado,
                    t.FechaFinEstimado,
                    t.Estado
                }).ToList();

                resultado.Add(new
                {
                    Proyecto = proyectoDto,
                    TareasAsignadas = tareasDto
                });
            }

            return Ok(resultado);
        }

        [HttpGet("archivadas")]
        [Authorize(Roles = "admin_empresa,superadmin")]
        public async Task<IActionResult> ObtenerTareasArchivadas()
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tareas = await _tareaService.ObtenerTareasArchivadasAsync(empresaId.Value);
            return Ok(tareas);
        }

        [HttpGet("por-proyecto/{proyectoId}/archivadas")]
        [Authorize(Roles = "admin_empresa,superadmin")]
        public async Task<IActionResult> ObtenerTareasArchivadasPorProyecto(int proyectoId)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            var tareas = await _tareaService.ObtenerTareasArchivadasPorProyectoAsync(proyectoId, empresaId.Value);
            return Ok(tareas);
        }
[HttpPost("{tareaId}/subtareas")]
[Authorize(Roles = "admin_empresa,superadmin")]
public async Task<IActionResult> CrearSubtarea(int tareaId, [FromBody] CreateSubtareaDto dto)
{
    var empresaId = GetEmpresaIdFromToken();
    if (empresaId == null) return Unauthorized();

    var subtarea = await _tareaService.CrearSubtareaAsync(tareaId, dto, empresaId.Value);
    if (subtarea == null) return BadRequest("No se pudo crear la subtarea.");

    await _auditoriaService.RegistrarEventoAsync(
        accion: "Crear Subtarea",
        entidad: "Subtarea",
        entidadId: subtarea.Id,
        descripcion: $"Se creó la subtarea '{subtarea.Descripcion}' para la tarea ID {tareaId}",
        generaNotificacion: true
    );

    return CreatedAtAction(nameof(ObtenerSubTareas), new { tareaId = tareaId }, subtarea);
}

[HttpPut("{tareaId}/subtareas/{subtareaId}/estado")]
public async Task<IActionResult> ActualizarSubtareaEstado(int tareaId, int subtareaId, [FromBody] SubtareaEstadoUpdateDto dto)
{
    var empresaId = GetEmpresaIdFromToken();
    if (empresaId == null) return Unauthorized();

    // Validar si la tarea y la subtarea pertenecen a la empresa
    bool existe = await _tareaService.VerificarSubtareaExisteAsync(tareaId, subtareaId, empresaId.Value);
    if (!existe) return NotFound();

    var resultado = await _tareaService.ActualizarEstadoSubtareaAsync(tareaId, subtareaId, dto.Completada);

    if (!resultado)
        return BadRequest("No se pudo actualizar la subtarea.");

    return NoContent();
}

// DTO para recibir el estado actualizado
public class SubtareaEstadoUpdateDto
{
    public bool Completada { get; set; }
}
    }
}