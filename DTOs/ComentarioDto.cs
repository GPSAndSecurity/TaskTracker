using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class ComentarioDto
    {
        public string UsuarioNombre { get; set; } = string.Empty;
        public string ComentarioTexto { get; set; } = string.Empty;
        public DateTime FechaComentario { get; set; }
    }
}