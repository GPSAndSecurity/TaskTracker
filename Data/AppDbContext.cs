using Microsoft.EntityFrameworkCore;
using TaskTracker.Models;

namespace TaskTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Cliente> Clientes { get; set; }

    public DbSet<Proyecto> Proyectos { get; set; }
    public DbSet<ProyectoColaborador> ProyectoColaboradores { get; set; }

    public DbSet<Tarea> Tareas { get; set; }
    public DbSet<TareaAsignado> TareaAsignados { get; set; }
    public DbSet<TareaAdjunto> TareaAdjuntos { get; set; }
    public DbSet<TareaComentario> TareaComentarios { get; set; }




//Crear las relaciones entre las tablas 
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

        // Proyecto ↔ Colaboradores
        modelBuilder.Entity<ProyectoColaborador>()
            .HasKey(pc => pc.Id);

        modelBuilder.Entity<ProyectoColaborador>()
            .HasOne(pc => pc.Proyecto)
            .WithMany(p => p.Colaboradores)
            .HasForeignKey(pc => pc.ProyectoId);

        modelBuilder.Entity<ProyectoColaborador>()
            .HasOne(pc => pc.Usuario)
            .WithMany()
            .HasForeignKey(pc => pc.UsuarioId);

        // Tarea ↔ Asignados
        modelBuilder.Entity<TareaAsignado>()
            .HasKey(ta => ta.Id);

        modelBuilder.Entity<TareaAsignado>()
            .HasOne(ta => ta.Tarea)
            .WithMany(t => t.Asignados)
            .HasForeignKey(ta => ta.TareaId);

        modelBuilder.Entity<TareaAsignado>()
            .HasOne(ta => ta.Usuario)
            .WithMany()
            .HasForeignKey(ta => ta.UsuarioId);

        // Tarea ↔ Adjuntos
        modelBuilder.Entity<TareaAdjunto>()
            .HasKey(ta => ta.Id);

        modelBuilder.Entity<TareaAdjunto>()
            .HasOne(ta => ta.Tarea)
            .WithMany(t => t.Adjuntos)
            .HasForeignKey(ta => ta.TareaId);

        // Tarea ↔ Comentarios
        modelBuilder.Entity<TareaComentario>()
            .HasKey(tc => tc.Id);

        modelBuilder.Entity<TareaComentario>()
            .HasOne(tc => tc.Tarea)
            .WithMany(t => t.Comentarios)
            .HasForeignKey(tc => tc.TareaId);

        modelBuilder.Entity<TareaComentario>()
            .HasOne(tc => tc.Usuario)
            .WithMany()
            .HasForeignKey(tc => tc.UsuarioId);
    }
}
