namespace TaskTracker.Models;

public class SubTarea

{

    public int Id { get; set; }



    public int TareaId { get; set; }

    public Tarea Tarea { get; set; } = null!;



    public string Descripcion { get; set; } = string.Empty;

    public bool Completada { get; set; } = false;

} 