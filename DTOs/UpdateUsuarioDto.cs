namespace TaskTracker.DTOs
{
    public class UpdateUsuarioDto
    {
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; } // opcional
        public string? Rol { get; set; }
        public int? EmpresaId { get; set; }
    }
}
