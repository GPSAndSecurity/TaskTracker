using TaskTracker.Models;

public class TareaTipoTrabajo
{
    public int DatosTecnicosId { get; set; }
    public DatosTecnicos DatosTecnicos { get; set; }

    public TipoTrabajo TipoTrabajo { get; set; } // El enum
}
