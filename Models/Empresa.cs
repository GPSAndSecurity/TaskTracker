namespace TaskTracker.Models;

public class Empresa
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    // Relaciones con otras tablas 
    public List<Usuario> Usuarios { get; set; } = new();
    public List<Cliente> Clientes { get; set; } = new();

}
