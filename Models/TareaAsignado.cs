namespace TaskTracker.Models;

public class TareaAsignado
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea? Tarea { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
