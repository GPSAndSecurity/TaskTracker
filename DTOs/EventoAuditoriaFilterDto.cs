public class EventoAuditoriaFilterDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Accion { get; set; }
    public int? UsuarioId { get; set; }
    public bool? SoloNotificaciones { get; set; }
    public bool? SoloHistorial { get; set; } // Opuesto a notificaciones
    public bool? NoVistas { get; set; }
}
