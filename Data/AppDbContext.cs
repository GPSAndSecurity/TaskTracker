using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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

    public DbSet<SubTarea> SubTareas { get; set; }

    public DbSet<Auditoria> Auditorias { get; set; }
public DbSet<Ubicacion> Ubicaciones { get; set; }
public DbSet<DatosTecnicos> DatosTecnicos { get; set; }
public DbSet<TareaTipoTrabajo> TareaTipoTrabajos { get; set; }

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

        modelBuilder.Entity<ProyectoColaborador>()
            .HasKey(pc => pc.Id);

        modelBuilder.Entity<ProyectoColaborador>()
            .HasOne(pc => pc.Proyecto)
            .WithMany(p => p.Colaboradores)
            .HasForeignKey(pc => pc.ProyectoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProyectoColaborador>()
            .HasOne(pc => pc.Usuario)
            .WithMany(u => u.ProyectosAsignados)
            .HasForeignKey(pc => pc.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TareaAsignado>()
            .HasKey(ta => ta.Id);

        modelBuilder.Entity<TareaAsignado>()
            .HasOne(ta => ta.Tarea)
            .WithMany(t => t.Asignados)
            .HasForeignKey(ta => ta.TareaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TareaAsignado>()
            .HasOne(ta => ta.Usuario)
            .WithMany()
            .HasForeignKey(ta => ta.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TareaAdjunto>()
            .HasKey(ta => ta.Id);

        modelBuilder.Entity<TareaAdjunto>()
            .HasOne(ta => ta.Tarea)
            .WithMany(t => t.Adjuntos)
            .HasForeignKey(ta => ta.TareaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TareaComentario>()
            .HasKey(tc => tc.Id);

        modelBuilder.Entity<TareaComentario>()
            .HasOne(tc => tc.Tarea)
            .WithMany(t => t.Comentarios)
            .HasForeignKey(tc => tc.TareaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TareaComentario>()
            .HasOne(tc => tc.Usuario)
            .WithMany()
            .HasForeignKey(tc => tc.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<Tarea>()
.HasOne(t => t.Cliente)
.WithMany()
.HasForeignKey(t => t.ClienteId)
.OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SubTarea>()
    .HasKey(st => st.Id);

        modelBuilder.Entity<SubTarea>()
            .HasOne(st => st.Tarea)
            .WithMany(t => t.SubTareas)
            .HasForeignKey(st => st.TareaId)
            .OnDelete(DeleteBehavior.Cascade);



        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Ubicacion)
            .WithMany() 
            .HasForeignKey(t => t.UbicacionId)
            .OnDelete(DeleteBehavior.SetNull);


        modelBuilder.Entity<Auditoria>()
            .HasOne(a => a.Usuario)
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TareaTipoTrabajo>()
               .HasKey(x => new { x.DatosTecnicosId, x.TipoTrabajo });

        modelBuilder.Entity<TareaTipoTrabajo>()
            .HasOne(x => x.DatosTecnicos)
            .WithMany(dt => dt.TiposTrabajo)
            .HasForeignKey(x => x.DatosTecnicosId);
        

        modelBuilder.Entity<DatosTecnicos>()
    .HasOne(dt => dt.Tarea)
    .WithMany(t => t.DatosTecnicos)
    .HasForeignKey(dt => dt.TareaId)
    .OnDelete(DeleteBehavior.Cascade);

    }

    // Método para agregar datos iniciales , despues de agregarlo se puede eliminar
    public static void Seed(AppDbContext context)
    {
        context.Database.Migrate(); 

        // Verificar si ya existe la empresa
        if (!context.Empresas.Any(e => e.Id == 1))
        {
            context.Empresas.Add(new Empresa
            {
                Id = 1,
                Nombre = "Empresa Principal"
            });
            context.SaveChanges();
        }

        // Verificar si ya existe un usuario superadmin 
        if (context.Usuarios.Any(u => u.Rol == "superadmin"))
        {
            Console.WriteLine("✅ Ya existe un usuario superadmin.");
            return;
        }

        try
        {
           string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}


            var superadmin = new Usuario
            {
                Name = "Admin",
                Lastname = "Principal",
                Email = "admin@gpsandsecurity.com",
                PasswordHash = HashPassword("admin123"),
                Rol = "superadmin",
                EmpresaId = 1
            };

            context.Usuarios.Add(superadmin);
            context.SaveChanges();

            Console.WriteLine("✅ Usuario superadmin creado correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al crear usuario superadmin: {ex.Message}");
        }
    }
}
