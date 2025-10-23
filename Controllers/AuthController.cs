using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.DTOs;

namespace TaskTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario == null || !usuario.Activo)
        {
            return Unauthorized("Credenciales incorrectas o usuario inactivo.");
        }

        bool loginConPasswordNormal = VerifyBCrypt(request.Password, usuario.PasswordHash);
        bool loginConPasswordTemporal = usuario.RequiereCambioPassword &&
                                         usuario.PasswordTemporalHash != null &&
                                         VerifyBCrypt(request.Password, usuario.PasswordTemporalHash);

        if (!loginConPasswordNormal && !loginConPasswordTemporal)
        {
            return Unauthorized("Credenciales incorrectas.");
        }

        var token = _jwtService.GenerateToken(usuario);

        return Ok(new
        {
            token,
            usuario = new
            {
                usuario.Id,
                usuario.Name,
                usuario.Lastname,
                usuario.Email,
                usuario.Rol,
                usuario.EmpresaId,
                requiereCambioPassword = usuario.RequiereCambioPassword
            }
        });
    }

[Authorize]
[HttpPost("cambiar-password")]
public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
{
    // Obtener el Id del usuario autenticado desde el token
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
        return Unauthorized("Usuario no autenticado.");

    if (!int.TryParse(userIdClaim.Value, out int userId))
        return Unauthorized("Id de usuario inválido.");

    var usuario = await _context.Usuarios.FindAsync(userId);
    if (usuario == null)
        return NotFound("Usuario no encontrado.");

    // Validación extra (opcional): solo permitir cambiar si se requiere
    if (!usuario.RequiereCambioPassword)
    {
        return BadRequest("El usuario no necesita cambiar la contraseña.");
    }

    usuario.PasswordHash = HashPasswordBCrypt(request.NuevaPassword);
    usuario.PasswordTemporalHash = null;
    usuario.RequiereCambioPassword = false;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Contraseña actualizada correctamente.",
        usuario.Id,
        usuario.Email
    });
}


    private string HashPasswordBCrypt(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyBCrypt(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    
[Authorize(Roles = "admin_empresa,superadmin")]
[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
    if (usuario == null)
        return NotFound("Usuario no encontrado.");

    // Generar contraseña temporal
    var nuevaPassword = GenerarContraseñaTemporal();

    // Guardar el hash de la contraseña temporal y marcar el cambio obligatorio
    usuario.PasswordTemporalHash = HashPasswordBCrypt(nuevaPassword);
    usuario.RequiereCambioPassword = true;

    await _context.SaveChangesAsync();

    // Devolver los datos necesarios para que el admin pueda comunicar al colaborador
    return Ok(new
    {
        usuario.Id,
        usuario.Name,
        usuario.Email,
        nuevaPassword
    });
}

private string GenerarContraseñaTemporal()
{
    const string caracteres = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
    var random = new Random();
    return new string(Enumerable.Repeat(caracteres, 8)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}

}
