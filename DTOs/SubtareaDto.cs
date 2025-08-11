using TaskTracker.Models;

namespace TaskTracker.DTOs
{
    public class SubTareaDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Completada { get; set; }
    }
}
