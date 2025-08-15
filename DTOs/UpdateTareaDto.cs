using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class UpdateTareaDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public DateTime? FechaInicioEstimado { get; set; }
        public DateTime? FechaFinEstimado { get; set; }
        public Prioridad Prioridad { get; set; } = Prioridad.Media;  //este es un enum
        public bool AttachmentRequerido { get; set; } = false;
        public bool UbicacionRequeridaAlCerrar { get; set; } = false;
    }
}
