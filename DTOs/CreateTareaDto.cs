using TaskTracker.Models;

public class CreateTareaDto
{
    public int ProyectoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int? UbicacionId { get; set; }
    public DateTime? FechaInicioEstimado { get; set; }
    public DateTime? FechaFinEstimado { get; set; }
    public Prioridad Prioridad { get; set; } = Prioridad.Media; //este es un enum
    public decimal Presupuesto { get; set; } = 0m;

public List<CreateDatosTecnicosDto>? DatosTecnicos { get; set; }
    public bool AttachmentRequerido { get; set; } = false;
    public bool UbicacionRequeridaAlCerrar { get; set; } = false;
}


public class CreateDatosTecnicosDto
{
    public string? VehiculoMarca { get; set; }
    public string? VehiculoModelo { get; set; }
    public string? VehiculoTipo { get; set; }
    public string? VehiculoCodigo { get; set; }
    public string? VehiculoPlaca { get; set; }
    public string? VehiculoVin { get; set; }
    public string? GpsSerie { get; set; }
    public string? GpsImei { get; set; }
    public string? SimCompania { get; set; }
    public string? SimCodigo { get; set; }
    public string? InstalacionAccesorios { get; set; }
    public string? TecnicoInstalador { get; set; }
    public string? FirmaCliente { get; set; }

    public List<string>? TiposTrabajo { get; set; }
}
