using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class ProyectoConTareasDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public List<TareaDetalleDto> Tareas { get; set; } = new();
    }


}
