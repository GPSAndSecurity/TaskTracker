namespace TaskTracker.DTOs;

public class CreateUsuarioDto
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "colaborador";
    public int? EmpresaId { get; set; }
}
