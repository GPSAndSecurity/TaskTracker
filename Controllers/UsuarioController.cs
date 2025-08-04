using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;

[Route("api/[controller]")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _service;

    public UsuariosController(UsuarioService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> CrearUsuario(CreateUsuarioDto dto)
    {
        var usuario = await _service.CrearUsuarioAsync(dto);
        return CreatedAtAction(nameof(CrearUsuario), new { id = usuario.Id }, usuario);
    }
}
