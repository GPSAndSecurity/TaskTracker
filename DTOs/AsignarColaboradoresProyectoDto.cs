namespace TaskTracker.DTOs;

public class AsignarColaboradoresProyectoDto
{
    public int ProyectoId { get; set; }
    public List<int> UsuarioIds { get; set; } = new();
}
