namespace TaskTracker.Models;

public class TareaComentario
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea? Tarea { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string ComentarioTexto { get; set; } = string.Empty;
    public DateTime FechaComentario { get; set; } = DateTime.UtcNow;
}