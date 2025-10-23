using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class TareaDetalleDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Detalles { get; set; } = string.Empty;

        public string? UbicacionNombre { get; set; }
        public Ubicacion? Ubicacion { get; set; }

        public DateTime? FechaInicioEstimado { get; set; }
        public DateTime? FechaFinEstimado { get; set; }
        public Prioridad Prioridad { get; set; }
        public EstadoTarea Estado { get; set; }
    public decimal Presupuesto { get; set; } = 0m;

public bool AttachmentRequerido { get; set; } = false;
    public bool UbicacionRequeridaAlCerrar { get; set; } = false;



        public List<ComentarioDto> Comentarios { get; set; } = new();
        public List<SubTareaDto> SubTareas { get; set; } = new();
        public List<AdjuntoDto> Adjuntos { get; set; } = new();
        public List<DatosTecnicosDto> DatosTecnicos { get; set; } = new();

 }
}
