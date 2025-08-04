using TaskTracker.Data;
using TaskTracker.DTOs;
using TaskTracker.Models;
using Microsoft.EntityFrameworkCore;

public class EmpresaService
{
    private readonly AppDbContext _context;

    public EmpresaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Empresa>> ObtenerTodasAsync()
    {
        return await _context.Empresas.ToListAsync();
    }

    public async Task<Empresa?> ObtenerPorIdAsync(int id)
    {
        return await _context.Empresas.FindAsync(id);
    }

    public async Task<Empresa> CrearEmpresaAsync(CreateEmpresaDto dto)
    {
        var empresa = new Empresa
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();

        return empresa;
    }

    public async Task<bool> ActualizarEmpresaAsync(int id, UpdateEmpresaDto dto)
    {
        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null) return false;

        empresa.Nombre = dto.Nombre;
        empresa.Descripcion = dto.Descripcion;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarEmpresaAsync(int id)
    {
        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null) return false;

        _context.Empresas.Remove(empresa);
        await _context.SaveChangesAsync();
        return true;
    }
}
