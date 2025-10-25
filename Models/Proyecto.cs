namespace TaskTracker.Models;

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activo { get; set; } = true;        
    public bool Archivado { get; set; } = false;    
    public List<ProyectoColaborador> Colaboradores { get; set; } = new();
    public List<Tarea> Tareas { get; set; } = new();



}
