public class TareasPorClienteDto
{
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;

    public int EnProceso { get; set; }
    public int Finalizadas { get; set; }
    public int Inconclusas { get; set; }
    public int Total { get; set; }
}
