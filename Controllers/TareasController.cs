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
        private readonly ClienteService _clienteService;
        private readonly S3Service _s3Service;

        public TareasController(TareaService tareaService, AuditoriaService auditoriaService, ClienteService clienteService, S3Service s3Service )
        {
            _tareaService = tareaService;
            _auditoriaService = auditoriaService;
            _clienteService = clienteService;
             _s3Service = s3Service;


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

            // Mapear comentarios con nombre de usuario
            var comentariosConNombre = tarea.Comentarios.Select(c => new
            {
                c.Id,
                c.TareaId,
                c.ComentarioTexto,
                c.FechaComentario,
                UsuarioNombre = c.Usuario != null ? $"{c.Usuario.Name} {c.Usuario.Lastname}" : "Desconocido"
            }).ToList();

            var colaboradores = await _tareaService.ObtenerColaboradoresPorTareaAsync(tareaId, empresaId.Value);

            // Mapear datos técnicos
            var datosTecnicos = tarea.DatosTecnicos?.Select(dt => new DatosTecnicosDto
            {
                Id = dt.Id,
                VehiculoMarca = dt.VehiculoMarca,
                VehiculoModelo = dt.VehiculoModelo,
                VehiculoTipo = dt.VehiculoTipo,
                VehiculoCodigo = dt.VehiculoCodigo,
                VehiculoPlaca = dt.VehiculoPlaca,
                VehiculoVin = dt.VehiculoVin,
                GpsSerie = dt.GpsSerie,
                GpsImei = dt.GpsImei,
                SimCompania = dt.SIMCompania,
                SimCodigo = dt.SIMCodigo,
                InstalacionAccesorios = dt.InstalacionAccesorios,
                TecnicoInstalador = dt.TecnicoInstalador,
                FirmaCliente = dt.FirmaCliente,
                TiposTrabajo = dt.TiposTrabajo.Select(tt => tt.TipoTrabajo.ToString()).ToList()
            }).ToList();

            var tareaDto = new
            {
                tarea.Id,
                tarea.Descripcion,
                tarea.Detalles,
                Ubicacion = tarea.Ubicacion != null ? new
                {
                    tarea.Ubicacion.Id,
                    tarea.Ubicacion.Nombre,
                    tarea.Ubicacion.Latitud,
                    tarea.Ubicacion.Longitud,
                } : null,
                tarea.Estado,
                tarea.FechaInicioEstimado,
                tarea.FechaFinEstimado,
                tarea.Prioridad,
                tarea.Presupuesto,
                tarea.AttachmentRequerido,
                tarea.UbicacionRequeridaAlCerrar,
                Comentarios = comentariosConNombre,
                Asignados = colaboradores,
                SubTareas = tarea.SubTareas.Select(st => new SubTareaDto
                {
                    Id = st.Id,
                    Descripcion = st.Descripcion,
                    Completada = st.Completada
                }).ToList(),
                Cliente = tarea.Cliente != null ? new
                {
                    tarea.Cliente.Id,
                    tarea.Cliente.Nombre,
                    tarea.Cliente.Encargado,
                    tarea.Cliente.Correo,
                    tarea.Cliente.Telefono
                } : null,
                ProyectoId = tarea.ProyectoId,
                DatosTecnicos = datosTecnicos
            };

            return Ok(tareaDto);
        }


        [HttpPost("{tareaId}/asignar-cliente")]
        [Authorize(Roles = "admin_empresa,superadmin,colaborador")]
        public async Task<IActionResult> AsignarCliente(int tareaId, [FromBody] AsignarClienteTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null || tareaId != dto.TareaId)
                return Unauthorized();

            var exito = await _tareaService.AsignarClienteATareaAsync(dto.TareaId, dto.ClienteId, empresaId.Value);
            if (!exito)
                return BadRequest("No se pudo asignar el cliente. Verifica que el cliente exista y pertenezca a la empresa.");

            // Obtener la tarea para la descripción
            var tarea = await _tareaService.ObtenerTareaDetalleAsync(dto.TareaId, empresaId.Value);
            var descripcionTarea = tarea?.Descripcion ?? "Descripción no disponible";

            string accion = dto.ClienteId.HasValue ? "Asignar Cliente" : "Desasignar Cliente";

            string descripcion;
            if (dto.ClienteId.HasValue)
            {
                // Obtener el cliente para mostrar el nombre
                var cliente = tarea?.Cliente;
                var nombreCliente = cliente?.Nombre ?? $"ID {dto.ClienteId}";
                descripcion = $"Se asignó el cliente \"{nombreCliente}\" a la tarea \"{descripcionTarea}\"";
            }
            else
            {
                descripcion = $"Se desasignó el cliente de la tarea \"{descripcionTarea}\"";
            }

            await _auditoriaService.RegistrarEventoAsync(
                accion: accion,
                entidad: "Tarea",
                entidadId: dto.TareaId,
                descripcion: descripcion,
                generaNotificacion: true
            );

            return NoContent();
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

            // Obtener la tarea para tener la descripción
            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);

            var descripcionTarea = tarea?.Descripcion ?? "Descripción no disponible";

            var descripcionEvento = $"Usuario '{usuarioNombre}' agregó un comentario a la tarea \"{descripcionTarea}\"";

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Agregar Comentario",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: descripcionEvento,
                generaNotificacion: false // No notificar al autor
            );

            await _auditoriaService.NotificarUsuariosRelacionadosConTareaAsync(
                tareaId,
                usuarioId.Value,
                "Comentario en tarea",
                descripcionEvento,
                "Tarea",
                tareaId
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
            var usuarioId = GetUsuarioIdFromToken();
            if (empresaId == null || usuarioId == null) return Unauthorized();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            if (tarea == null) return NotFound("Tarea no encontrada.");

            var estadoAnterior = tarea.Estado;

            if (nuevoEstado == EstadoTarea.Finalizada)
            {
                var subtareas = await _tareaService.ObtenerSubTareasPorTareaAsync(tareaId, empresaId.Value);
                if (subtareas.Any(st => !st.Completada))
                {
                    return BadRequest("No se puede finalizar la tarea porque tiene subtareas pendientes.");
                }
            }

            var exito = await _tareaService.CambiarEstadoTareaAsync(tareaId, nuevoEstado, empresaId.Value);
            if (!exito) return BadRequest("No se pudo cambiar el estado.");

            var descripcion = $"Se cambió el estado de la tarea '{tarea.Descripcion}' de '{estadoAnterior}' a '{nuevoEstado}'";

            // Registrar evento para historial sin notificar al autor
            await _auditoriaService.RegistrarEventoAsync(
                accion: "Cambiar Estado",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: descripcion,
                generaNotificacion: false
            );

            // Notificar a los demás (excepto el que cambió el estado)
            await _auditoriaService.NotificarUsuariosRelacionadosConTareaAsync(
                tareaId,
                usuarioId.Value,
                "Cambio de estado en tarea", 
                descripcion,
                "Tarea",
                tareaId
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

            // Obtener la tarea para la descripción (antes de eliminar)
            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            var descripcionTarea = tarea?.Descripcion ?? "Descripción no disponible";

            // Eliminar la tarea
            var exito = await _tareaService.EliminarTareaAsync(tareaId, empresaId.Value);
            if (!exito) return NotFound();

            // Registrar evento en auditoría
            string descripcion = $"Se eliminó la tarea \"{descripcionTarea}\" (ID {tareaId})";

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Eliminar Tarea",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: descripcion,
                generaNotificacion: true
            );

            return NoContent();
        }


        [HttpPost("{tareaId}/asignar")]
        [Authorize(Roles = "admin_empresa,superadmin, colaborador")]
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

        [HttpPut("{tareaId}/cliente")]
        [Authorize(Roles = "superadmin, admin_empresa, colaboradores")]
        public async Task<IActionResult> AsignarClienteATarea(int tareaId, [FromBody] AsignarClienteTareaDto dto)
        {
            var empresaId = GetEmpresaIdFromToken();
            if (empresaId == null) return Unauthorized();

            if (tareaId != dto.TareaId)
                return BadRequest("El ID de la tarea en la URL no coincide con el del cuerpo.");

            var exito = await _tareaService.AsignarClienteATareaAsync(dto, empresaId.Value);
            if (!exito) return BadRequest("No se pudo asignar el cliente a la tarea.");

            // Obtener la tarea con detalles (incluye descripción)
            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
            if (tarea == null)
                return NotFound("Tarea no encontrada.");

            string clienteNombre = "Cliente desasignado";
            if (dto.ClienteId.HasValue)
            {
                var cliente = await _clienteService.ObtenerClientePorIdAsync(dto.ClienteId.Value, empresaId.Value);
                clienteNombre = cliente != null ? cliente.Nombre : $"Cliente ID {dto.ClienteId.Value}";
            }

            var descripcion = dto.ClienteId.HasValue
                ? $"Se asignó el cliente \"{clienteNombre}\" a la tarea \"{tarea.Descripcion}\""
                : $"Se desasignó el cliente de la tarea \"{tarea.Descripcion}\"";

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Asignar Cliente",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: descripcion,
                generaNotificacion: true
            );

            return Ok("Cliente asignado correctamente.");
        }


        [HttpGet("{tareaId}/colaboradores")]
        [Authorize(Roles = "admin_empresa,superadmin, colaborador")]

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

            // Actualizar propiedades de la tarea
            tarea.Descripcion = dto.Descripcion;
            tarea.Detalles = dto.Detalles;
            tarea.UbicacionId = dto.UbicacionId;
            tarea.FechaInicioEstimado = dto.FechaInicioEstimado;
            tarea.FechaFinEstimado = dto.FechaFinEstimado;
            tarea.Presupuesto = dto.Presupuesto;
            tarea.Prioridad = dto.Prioridad;
            tarea.AttachmentRequerido = dto.AttachmentRequerido;
            tarea.UbicacionRequeridaAlCerrar = dto.UbicacionRequeridaAlCerrar;

            await _tareaService.ActualizarTareaAsync(tarea);

            // Procesar Datos Técnicos (si vienen en el DTO)
            if (dto.DatosTecnicos != null)
            {
                // Eliminar existentes
                await _tareaService.EliminarDatosTecnicosPorTareaAsync(tarea.Id);

                // Agregar nuevos
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
                            TipoTrabajo = Enum.Parse<TipoTrabajo>(tipoStr, ignoreCase: true)
                        }).ToList() ?? new()
                    };

                    await _tareaService.AgregarDatosTecnicosAsync(datosTecnicos);
                }
            }

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
    try
    {
        // VALIDACIONES INICIALES DE EMPRESAS Y ESO
        var empresaId = GetEmpresaIdFromToken();
        if (empresaId == null)
            return Unauthorized("Token inválido.");

        var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId.Value);
        if (tarea == null)
            return NotFound("La tarea no existe o no pertenece a esta empresa.");

        if (archivo == null || archivo.Length == 0)
            return BadRequest("Archivo inválido.");

        if (archivo.Length > 10 * 1024 * 1024) // 10MB
            return BadRequest("El archivo excede el tamaño máximo permitido (10MB).");

        // OBTENER NOMBRES DE CARPETAS
        var empresaNombre = await _tareaService.ObtenerNombreEmpresaPorId(empresaId.Value);
        var proyectoNombre = await _tareaService.ObtenerNombreProyectoPorId(tarea.ProyectoId);

        if (string.IsNullOrEmpty(empresaNombre) || string.IsNullOrEmpty(proyectoNombre))
            return BadRequest("No se pudo determinar empresa o proyecto.");

        // Sanitizar
        var empresaFolder = empresaNombre.Replace(" ", "_").Trim();
        var proyectoFolder = proyectoNombre.Replace(" ", "_").Trim();


        // PREPARAR ARCHIVO A SUBIR
        var extension = Path.GetExtension(archivo.FileName).ToLower();
        var nuevoNombre = $"{Guid.NewGuid()}{extension}";
        var s3Key = $"{empresaFolder}/{proyectoFolder}/{nuevoNombre}";

        Stream finalStream;

        if (extension is ".jpg" or ".jpeg" or ".png")
        {
            using var originalStream = archivo.OpenReadStream();
            var compressedStream = new MemoryStream();

            // COMPRIMIR IMAGEN
            await _tareaService.ComprimirImagenStreamAsync(originalStream, compressedStream, extension);

            compressedStream.Position = 0;
            finalStream = compressedStream;
        }
        else
        {
            // COPIAR ARCHIVO NORMAL
            finalStream = new MemoryStream();
            await archivo.CopyToAsync(finalStream);
            finalStream.Position = 0;
        }


        // SUBIR A AWS S3
        string url;
        try
        {
            url = await _s3Service.UploadFileAsync(finalStream, s3Key, archivo.ContentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al subir archivo a S3: {ex.Message}");
        }

        // GUARDAR EN LA BASE DE DATOS
        
        var adjunto = await _tareaService.AgregarAdjuntoAsync(
            tareaId,
            url,
            archivo.FileName
        );

        // RESPUESTA FINAL
        return Ok(new
        {
            mensaje = "Archivo subido correctamente.",
            url,
            nombre_original = archivo.FileName,
            nombre_alojado = nuevoNombre,
            tareaId,
            proyecto = proyectoNombre,
            empresa = empresaNombre,
            registro = adjunto
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Error interno: {ex.Message}");
    }
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

    // Convertir URL normal a Presigned URL
    foreach (var item in lista)
    {
        if (!string.IsNullOrWhiteSpace(item.ArchivoUrl))
        {
            var key = ObtenerKeyDesdeUrl(item.ArchivoUrl);
            item.ArchivoUrl = _s3Service.GetPresignedUrl(key, minutes: 60);
        }
    }

    return Ok(lista);
}
private string ObtenerKeyDesdeUrl(string url)
{
    if (string.IsNullOrWhiteSpace(url))
        return null;

    var uri = new Uri(url);

    /// 1. Formato tipo: bucket.s3.amazonaws.com/key
    // 2. Formato tipo: s3-myBucket.s3.amazonaws.com/key
    if (uri.Host.Contains(".s3.amazonaws.com"))
    {
        return uri.AbsolutePath.TrimStart('/');
    }

    // 3. Formatos tipo: s3.region.amazonaws.com/bucket/key (por si AWS cambia)
    if (uri.Host.StartsWith("s3") && uri.Segments.Length > 2)
    {
        // Une todos los segmentos después del bucket
        return string.Join("", uri.Segments.Skip(2)).TrimStart('/');
    }

    // 4. Fallback seguro
    return uri.AbsolutePath.TrimStart('/');
}
        [HttpPut("{tareaId}/archivar")]
        [Authorize(Roles = "superadmin,admin_empresa")]
        public async Task<IActionResult> ArchivarTarea(int tareaId)
        {
            int empresaId = ObtenerEmpresaIdDesdeToken();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId);
            if (tarea == null)
                return BadRequest("Tarea no encontrada o no pertenece a la empresa.");

            var resultado = await _tareaService.ArchivarTareaAsync(tareaId);
            if (!resultado)
                return BadRequest("Tarea ya archivada o no se puede archivar.");

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Archivar Tarea",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: $"Se archivó la tarea: '{tarea.Descripcion}'",
                generaNotificacion: true
            );

            return Ok("Tarea archivada correctamente.");
        }

        [HttpPut("{tareaId}/desarchivar")]
        [Authorize(Roles = "superadmin,admin_empresa")]
        public async Task<IActionResult> DesarchivarTarea(int tareaId)
        {
            int empresaId = ObtenerEmpresaIdDesdeToken();

            var tarea = await _tareaService.ObtenerTareaDetalleAsync(tareaId, empresaId);
            if (tarea == null)
                return BadRequest("Tarea no encontrada o no pertenece a la empresa.");

            var resultado = await _tareaService.DesarchivarTareaAsync(tareaId);
            if (!resultado)
                return BadRequest("No se pudo desarchivar la tarea.");

            await _auditoriaService.RegistrarEventoAsync(
                accion: "Desarchivar Tarea",
                entidad: "Tarea",
                entidadId: tareaId,
                descripcion: $"Se desarchivó la tarea: '{tarea.Descripcion}'",
                generaNotificacion: true
            );

            return Ok("Tarea desarchivada correctamente.");
        }

        // Método para obtener empresaId del token (ya lo debes tener implementado)
        private int ObtenerEmpresaIdDesdeToken()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "empresaId")?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int empresaId))
                throw new UnauthorizedAccessException("empresaId inválido en el token");

            return empresaId;
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
        
        [HttpDelete("{tareaId}/subtareas/{subtareaId}")]
[Authorize(Roles = "admin_empresa,superadmin")]
public async Task<IActionResult> EliminarSubtarea(int tareaId, int subtareaId)
{
    var empresaId = GetEmpresaIdFromToken();
    if (empresaId == null) return Unauthorized();

    // Validar si la subtarea pertenece a la empresa
    bool existe = await _tareaService.VerificarSubtareaExisteAsync(tareaId, subtareaId, empresaId.Value);
    if (!existe) return NotFound();

    // Eliminar la subtarea
    bool eliminado = await _tareaService.EliminarSubtareaAsync(tareaId, subtareaId);
    if (!eliminado) return BadRequest("No se pudo eliminar la subtarea.");


    await _auditoriaService.RegistrarEventoAsync(
        accion: "Eliminar Subtarea",
        entidad: "Subtarea",
        entidadId: subtareaId,
        descripcion: $"Se eliminó la subtarea con ID {subtareaId} de la tarea ID {tareaId}",
        generaNotificacion: false
    );

    return NoContent();
}

        
    }
      
}