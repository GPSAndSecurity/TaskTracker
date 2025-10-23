public class ProyectoConAvanceDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public double PorcentajeAvance { get; set; }
    public bool Archivado { get; set; } // 👈 Asegúrate de incluir esto
    public int TotalTareas { get; set; }



}
