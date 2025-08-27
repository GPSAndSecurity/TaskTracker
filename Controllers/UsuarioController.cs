using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _service;

    public UsuariosController(UsuarioService service)
    {
        _service = service;
    }

    // Crear un nuevo usuario
    [HttpPost]
    public async Task<ActionResult<Usuario>> CrearUsuario(CreateUsuarioDto dto)
    {
        var usuario = await _service.CrearUsuarioAsync(dto);
        return CreatedAtAction(nameof(ObtenerUsuarioPorId), new { id = usuario.Id }, usuario);
    }

    // Obtener usuario por ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Usuario>> ObtenerUsuarioPorId(int id)
    {
        var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
        if (usuario == null)
            return NotFound();

        return Ok(usuario);
    }

    // Obtener todos los usuarios (solo superadmin o admin_empresa)
    [HttpGet]
[Authorize(Roles = "superadmin,admin_empresa")]
public async Task<ActionResult<IEnumerable<Usuario>>> ObtenerUsuarios([FromQuery] bool incluirInactivos = false)
{
    var rol = User.FindFirst(ClaimTypes.Role)?.Value;
    var empresaIdStr = User.FindFirst("empresaId")?.Value;

    if (rol == "superadmin")
    {
        var usuarios = await _service.ObtenerTodosAsync(incluirInactivos);
        return Ok(usuarios);
    }

    if (rol == "admin_empresa" && int.TryParse(empresaIdStr, out var empresaId))
    {
        var colaboradores = await _service.ObtenerColaboradoresPorEmpresaAsync(empresaId, incluirInactivos);
        return Ok(colaboradores);
    }

    return Forbid();
}

    // Obtener colaboradores de la empresa del usuario autenticado
    [HttpGet("colaboradores")]
    [Authorize(Roles = "admin_empresa,superadmin")]
    public async Task<ActionResult<IEnumerable<Usuario>>> ObtenerColaboradores()
    {
        var empresaIdStr = User.FindFirst("empresaId")?.Value;
        if (!int.TryParse(empresaIdStr, out var empresaId))
            return Unauthorized("Empresa no encontrada en el token.");

        var colaboradores = await _service.ObtenerColaboradoresPorEmpresaAsync(empresaId);
        return Ok(colaboradores);
    }

    //  total de colaboradores por empresa
    [HttpGet("total-colaboradores")]
    [Authorize(Roles = "admin_empresa,superadmin")]
    public async Task<ActionResult<int>> GetTotalColaboradores()
    {
        var empresaIdStr = User.FindFirst("empresaId")?.Value;
        if (!int.TryParse(empresaIdStr, out var empresaId))
            return Unauthorized("Empresa no encontrada en el token.");

        int total = await _service.ContarColaboradoresPorEmpresaAsync(empresaId);
        return Ok(total);
    }

    // Obtener perfil del usuario autenticado
    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idStr, out var id))
            return Unauthorized();

        var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
        if (usuario == null)
            return NotFound();

        return Ok(new
        {
            usuario.Id,
            usuario.Name,
            usuario.Lastname,
            usuario.Email,
            usuario.Rol,
            usuario.EmpresaId
        });
    }
    //actualizar usuario 
    [HttpPut("{id}")]
    [Authorize(Roles = "superadmin,admin_empresa")]
    public async Task<IActionResult> ActualizarUsuario(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
        if (usuario == null)
            return NotFound();

        var actualizado = await _service.ActualizarUsuarioAsync(id, dto);
        return Ok(actualizado);
    }
    //eliminar usuario 
   // [HttpDelete("{id}")]
    //[Authorize(Roles = "superadmin")]
    //public async Task<IActionResult> EliminarUsuario(int id)
    //{
      //  var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
        //if (usuario == null)
          //  return NotFound();

  //      await _service.EliminarUsuarioAsync(id);
    //    return NoContent();
    //}

    // Inactivar un usuario (superadmin o admin_empresa)
[HttpPatch("{id}/inactivar")]
[Authorize(Roles = "superadmin,admin_empresa")]
public async Task<IActionResult> InactivarUsuario(int id)
{
    var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
    if (usuario == null)
        return NotFound($"Usuario con ID {id} no encontrado.");

    if (!usuario.Activo)
        return BadRequest("El usuario ya está inactivo.");

    usuario.Activo = false;
    await _service.GuardarCambiosAsync();

    return Ok("Usuario inactivado correctamente.");
}

[HttpPatch("{id}/activar")]
[Authorize(Roles = "superadmin,admin_empresa")]
public async Task<IActionResult> ActivarUsuario(int id)
{
    var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
    if (usuario == null)
        return NotFound("Usuario no encontrado.");

    if (usuario.Activo)
        return BadRequest("El usuario ya está activo.");

    usuario.Activo = true;
    await _service.GuardarCambiosAsync();

    return Ok("Usuario activado correctamente.");
}

}
