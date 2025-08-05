using TaskTracker.Models;

public class CreateTareaDto
{
    public int ProyectoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }
    public DateTime? FechaInicioEstimado { get; set; }
    public DateTime? FechaFinEstimado { get; set; }
    public Prioridad Prioridad { get; set; } = Prioridad.Media; // ENUM aquí
    public bool AttachmentRequerido { get; set; } = false;
    public bool UbicacionRequeridaAlCerrar { get; set; } = false;
}
