namespace TaskTracker.DTOs;

public class CreateClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Encargado { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
}
