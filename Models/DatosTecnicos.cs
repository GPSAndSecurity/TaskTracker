namespace TaskTracker.Models;

public class DatosTecnicos
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;

    // Relación con tipos de trabajo (muchos a muchos)
    public List<TareaTipoTrabajo> TiposTrabajo { get; set; } = new();

    // Vehículo
    public string? VehiculoMarca { get; set; }
    public string? VehiculoModelo { get; set; }
    public string? VehiculoTipo { get; set; }
    public string? VehiculoCodigo { get; set; }
    public string? VehiculoPlaca { get; set; }
    public string? VehiculoVin { get; set; }

    // GPS
    public string? GpsSerie { get; set; }
    public string? GpsImei { get; set; }

    // SIM
    public string? SIMCompania { get; set; }
    public string? SIMCodigo { get; set; }

    // Instalación
    public string? InstalacionAccesorios { get; set; }
    public string? TecnicoInstalador { get; set; }

    // Firma
    public string? FirmaCliente { get; set; }
}
