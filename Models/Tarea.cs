namespace TaskTracker.Models;

public class Tarea
{
    public int Id { get; set; }

    public int ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }

    public DateTime? FechaInicioEstimado { get; set; }
    public DateTime? FechaFinEstimado { get; set; }

    public Prioridad Prioridad { get; set; } = Prioridad.Media;

    public bool AttachmentRequerido { get; set; } = false;
    public bool UbicacionRequeridaAlCerrar { get; set; } = false;

    public DateTime? FechaCierre { get; set; }

    public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;

    public List<TareaAsignado> Asignados { get; set; } = new();
    public List<TareaAdjunto> Adjuntos { get; set; } = new();
    public List<TareaComentario> Comentarios { get; set; } = new();
}
