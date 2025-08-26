public class ComentarioAdjuntoDto
{
    public bool EsAdjunto { get; set; } // true si es un adjunto, false si es comentario
    public string UsuarioNombre { get; set; } = string.Empty; // solo para comentario
    public string ComentarioTexto { get; set; } = string.Empty; // solo para comentario
    public DateTime FechaComentario { get; set; }

    // Propiedades para adjunto
    public int? AdjuntoId { get; set; }
    public string ArchivoUrl { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
}