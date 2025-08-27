namespace TaskTracker.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "colaborador"; // superadmin, admin_empresa, colaborador
    public bool Activo { get; set; } = true;

    public int? EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    public List<ProyectoColaborador> ProyectosAsignados { get; set; } = new();

}