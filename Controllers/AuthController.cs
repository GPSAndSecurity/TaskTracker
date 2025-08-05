using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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

        if (usuario == null || !VerifyPassword(request.Password, usuario.PasswordHash))
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
                usuario.EmpresaId
            }
        });
    }

    private bool VerifyPassword(string plainPassword, string storedHash)
    {
        using var sha256 = SHA256.Create();
        var hashed = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(plainPassword)));
        return hashed == storedHash;
    }
}
