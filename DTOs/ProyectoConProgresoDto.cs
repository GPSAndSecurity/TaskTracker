public class ProyectoConAvanceDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;  // nueva
    public DateTime? FechaInicio { get; set; }                // nueva
    public DateTime? FechaFin { get; set; }                   // nueva
    public double PorcentajeAvance { get; set; }
}
