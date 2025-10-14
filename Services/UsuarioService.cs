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

    // Crear un nuevo usuario
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
            EmpresaId = dto.EmpresaId,
            Activo = true,
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    // Obtener un usuario por ID
    public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    // Obtener todos los usuarios
   public async Task<List<Usuario>> ObtenerTodosAsync(bool incluirInactivos = false)
{
    if (incluirInactivos)
        return await _context.Usuarios.ToListAsync();

    return await _context.Usuarios.Where(u => u.Activo).ToListAsync();
}

public async Task<List<Usuario>> ObtenerColaboradoresPorEmpresaAsync(int empresaId, bool incluirInactivos = false)
{
    if (incluirInactivos)
        return await _context.Usuarios.Where(u => u.EmpresaId == empresaId).ToListAsync();

    return await _context.Usuarios.Where(u => u.EmpresaId == empresaId && u.Activo).ToListAsync();
}
    // Actualizar un usuario existente
    public async Task<Usuario?> ActualizarUsuarioAsync(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return null;

        // Actualizar solo campos que no son nulos
        if (!string.IsNullOrEmpty(dto.Name)) usuario.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Lastname)) usuario.Lastname = dto.Lastname;
        if (!string.IsNullOrEmpty(dto.Email)) usuario.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Rol)) usuario.Rol = dto.Rol;
        if (dto.EmpresaId.HasValue) usuario.EmpresaId = dto.EmpresaId;

        // Actualizar contraseña solo si se proporciona
        if (!string.IsNullOrEmpty(dto.Password))
        {
            usuario.PasswordHash = HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();
        return usuario;
    }

   
    // Hash de la contraseña, usando el SHA256 porque segun es mejor bcrypt. es mas rapido y seguro que bcrypt 
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public async Task<int> ContarColaboradoresPorEmpresaAsync(int empresaId)
    {
        return await _context.Usuarios
            .Where(u => u.EmpresaId == empresaId && u.Rol == "colaborador" && u.Activo )
            .CountAsync();
    }


   public async Task<bool> InactivarUsuarioAsync(int id)
{
    var usuario = await _context.Usuarios.FindAsync(id);
    if (usuario == null)
        return false;

    if (!usuario.Activo)
        return false; // Usuario ya inactivo

    usuario.Activo = false;
    await _context.SaveChangesAsync();
    return true;
}

public async Task GuardarCambiosAsync()
{
    await _context.SaveChangesAsync();
}
public async Task<bool> ActivarUsuarioAsync(int id)
{
    var usuario = await _context.Usuarios.FindAsync(id);
    if (usuario == null || usuario.Activo)
        return false;

    usuario.Activo = true;
    return true;
}



}
