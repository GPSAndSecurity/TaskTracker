public class TareasPorUsuarioDto
{
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; }
    public string UsuarioApellido { get; set; }
    public int? EmpresaId { get; set; }  

    public int EnProceso { get; set; }
    public int Finalizadas { get; set; }
    public int Inconclusas { get; set; }
    public int Total { get; set; }
}
