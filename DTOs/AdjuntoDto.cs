using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class AdjuntoDto
    {
        public int Id { get; set; }
        public string ArchivoUrl { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; }
    }
}