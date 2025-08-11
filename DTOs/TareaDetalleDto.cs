using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class TareaDetalleDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public DateTime? FechaInicioEstimado { get; set; }
        public DateTime? FechaFinEstimado { get; set; }
        public Prioridad Prioridad { get; set; }
        public EstadoTarea Estado { get; set; }

        public List<ComentarioDto> Comentarios { get; set; } = new();
        public List<SubTareaDto> SubTareas { get; set; } = new();
        public List<AdjuntoDto> Adjuntos { get; set; } = new();
    }
}

