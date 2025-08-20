namespace TaskTracker.Models;

public class TareaAdjunto
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea? Tarea { get; set; }

    public string ArchivoUrl { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
}