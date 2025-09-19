using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public class ClienteService
    {
        private readonly AppDbContext _context;

        public ClienteService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Obtener lista de clientes por empresa
        public async Task<List<Cliente>> ObtenerClientesPorEmpresaAsync(int empresaId)
        {
            return await _context.Clientes
                .Where(c => c.EmpresaId == empresaId)
                .ToListAsync();
        }

        // Crear cliente
        public async Task<Cliente> CrearClienteAsync(CreateClienteDto dto)
        {
            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                Encargado = dto.Encargado,  
                EmpresaId = dto.EmpresaId
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        // Obtener cliente por id y empresa
public async Task<Cliente?> ObtenerClientePorIdAsync(int id, int empresaId)
{
    return await _context.Clientes
        .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
}


        // Actualizar cliente
public async Task<bool> ActualizarClienteAsync(int id, UpdateClienteDto dto, int empresaId)
{
    var cliente = await ObtenerClientePorIdAsync(id, empresaId);
    if (cliente == null) return false;

    cliente.Nombre = dto.Nombre;
    cliente.Correo = dto.Correo;
    cliente.Encargado = dto.Encargado;  
    cliente.Telefono = dto.Telefono;

    await _context.SaveChangesAsync();
    return true;
}

// Eliminar cliente
public async Task<bool> EliminarClienteAsync(int id, int empresaId)
{
    var cliente = await ObtenerClientePorIdAsync(id, empresaId);
    if (cliente == null) return false;

    _context.Clientes.Remove(cliente);
    await _context.SaveChangesAsync();
    return true;
}

        // contar clientes por empresa
        public async Task<int> ContarClientesPorEmpresaAsync(int empresaId)
        {
            return await _context.Clientes
                                 .Where(c => c.EmpresaId == empresaId)
                                 .CountAsync();
        }
    }
}
