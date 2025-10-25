public class ComentarioAdjuntoDto
{
    public bool EsAdjunto { get; set; } 
    public string UsuarioNombre { get; set; } = string.Empty; 
    public string ComentarioTexto { get; set; } = string.Empty; 
    public DateTime FechaComentario { get; set; }

    // Propiedades para adjunto
    public int? AdjuntoId { get; set; }
    public string ArchivoUrl { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
}