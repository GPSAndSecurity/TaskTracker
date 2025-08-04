using Microsoft.EntityFrameworkCore;
using TaskTracker.Models;

namespace TaskTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Cliente> Clientes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Empresa>()
            .HasMany(e => e.Usuarios)
            .WithOne(u => u.Empresa)
            .HasForeignKey(u => u.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Empresa>()
            .HasMany(e => e.Clientes)
            .WithOne(c => c.Empresa)
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
