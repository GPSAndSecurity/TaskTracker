namespace TaskTracker.DTOs
{
    public class DatosTecnicosDto
    {
        public int Id { get; set; }

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
        public string? SimCompania { get; set; }
        public string? SimCodigo { get; set; }

        // Instalación
        public string? InstalacionAccesorios { get; set; }
        public string? TecnicoInstalador { get; set; }

        // Firma
        public string? FirmaCliente { get; set; }

        // Tipos de trabajo (como lista de strings)
        public List<string> TiposTrabajo { get; set; } = new();
    }
}
