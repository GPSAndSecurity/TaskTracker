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
                EmpresaId = dto.EmpresaId
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        // Obtener cliente por id
        public async Task<Cliente?> ObtenerClientePorIdAsync(int id)
        {
            return await _context.Clientes.FindAsync(id);
        }

        // Actualizar cliente
        public async Task<bool> ActualizarClienteAsync(int id, UpdateClienteDto dto)
        {
            var cliente = await ObtenerClientePorIdAsync(id);
            if (cliente == null) return false;

            cliente.Nombre = dto.Nombre;
            cliente.Correo = dto.Correo;
            cliente.Telefono = dto.Telefono;

            await _context.SaveChangesAsync();
            return true;
        }

        // Eliminar cliente
        public async Task<bool> EliminarClienteAsync(int id)
        {
            var cliente = await ObtenerClientePorIdAsync(id);
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
