namespace TaskTracker.DTOs
{
    public class UpdateProyectoDto
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        // Agrega más propiedades según lo que se permita actualizar
    }
}
