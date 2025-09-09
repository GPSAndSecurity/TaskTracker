using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;

public class UbicacionService
{
    private readonly AppDbContext _context;

    public UbicacionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Ubicacion?> ObtenerPorIdAsync(int id, int empresaId)
    {
        return await _context.Ubicaciones
            .FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId);
    }

    public async Task<List<Ubicacion>> ObtenerTodasAsync(int empresaId)
    {
        return await _context.Ubicaciones
            .Where(u => u.EmpresaId == empresaId)
            .ToListAsync();
    }

    public async Task<Ubicacion> CrearAsync(Ubicacion ubicacion)
    {
        _context.Ubicaciones.Add(ubicacion);
        await _context.SaveChangesAsync();
        return ubicacion;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var ubicacion = await _context.Ubicaciones.FindAsync(id);
        if (ubicacion == null) return false;

        _context.Ubicaciones.Remove(ubicacion);
        await _context.SaveChangesAsync();
        return true;
    }
}
