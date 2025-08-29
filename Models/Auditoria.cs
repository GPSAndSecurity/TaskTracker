namespace TaskTracker.Models
{
    public class Auditoria
    {
        public int Id { get; set; }

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public int? EntidadId { get; set; }
        public string? Descripcion { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;


    }
}
