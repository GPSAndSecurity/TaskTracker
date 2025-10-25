//esto no esa bien implementado ya que solo lo tengo para mostrar las notificaciones,
//si se hacen los logs en la tabla pero no se muestran aun. 
public class EventoAuditoriaFilterDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Accion { get; set; }
    public int? UsuarioId { get; set; }
    public bool? SoloNotificaciones { get; set; }
    public bool? SoloHistorial { get; set; }
    public bool? NoVistas { get; set; }
}
