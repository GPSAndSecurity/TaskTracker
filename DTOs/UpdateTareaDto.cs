using TaskTracker.Models;

namespace TaskTracker.DTOs
{
   public class UpdateTareaDto
{
    public string Descripcion { get; set; } = string.Empty;
    public int? UbicacionId { get; set; }     // solo el id
    public DateTime? FechaInicioEstimado { get; set; }
    public DateTime? FechaFinEstimado { get; set; }
    public Prioridad Prioridad { get; set; } = Prioridad.Media;
        public decimal Presupuesto { get; set; } = 0m;

public List<CreateDatosTecnicosDto>? DatosTecnicos { get; set; }

    public bool AttachmentRequerido { get; set; } = false;
    public bool UbicacionRequeridaAlCerrar { get; set; } = false;
}
}
