public class AsignarClienteTareaDto
{
    public int TareaId { get; set; }

    public int? ClienteId { get; set; }  // Nullable para poder desasignar también
}
