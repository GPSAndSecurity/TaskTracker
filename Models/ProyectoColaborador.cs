namespace TaskTracker.Models;

public class ProyectoColaborador
{
    public int Id { get; set; }  

    public int ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; } 

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}

