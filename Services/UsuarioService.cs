using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
    {
         return await _context.Usuarios.FindAsync(id);
    }

    public async Task<List<Usuario>> ObtenerTodosAsync()
    {
        return await _context.Usuarios.ToListAsync();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}
