using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;
using System.Security.Claims;

namespace TaskTracker.Controllers;

[Route("api/[controller]")]
[ApiController]
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
    [Authorize]
    public async Task<ActionResult<Usuario>> ObtenerUsuarioPorId(int id)
    {
        var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
        if (usuario == null)
            return NotFound();

        return Ok(usuario);
    }

    // Obtener todos los usuarios (solo superadmin)
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public async Task<ActionResult<IEnumerable<Usuario>>> ObtenerTodos()
    {
        var usuarios = await _service.ObtenerTodosAsync();
        return Ok(usuarios);
    }

    // Obtener el perfil del usuario autenticado
    [HttpGet("perfil")]
    [Authorize]
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


    [HttpDelete("{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _service.ObtenerUsuarioPorIdAsync(id);
        if (usuario == null)
            return NotFound();

        await _service.EliminarUsuarioAsync(id);
        return NoContent();
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

}
