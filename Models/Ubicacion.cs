using TaskTracker.Models;

public class Ubicacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public double Latitud { get; set; }    // Y
    public double Longitud { get; set; }   // X

    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
