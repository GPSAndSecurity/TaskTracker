using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;
using System.Security.Cryptography;
using System.Text;

public class UsuarioService
{
    private readonly AppDbContext _context;

    public UsuarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> CrearUsuarioAsync(CreateUsuarioDto dto)
    {
        var passwordHash = HashPassword(dto.Password);

        var usuario = new Usuario
        {
            Name = dto.Name,
            Lastname = dto.Lastname,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Rol = dto.Rol,
            EmpresaId = dto.EmpresaId
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}
