using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;


[Route("api/[controller]")]
[ApiController]
public class EmpresasController : ControllerBase
{
    private readonly EmpresaService _service;
    private readonly AuditoriaService _auditoria;

    public EmpresasController(EmpresaService service, AuditoriaService auditoria)
{
    _service = service;
    _auditoria = auditoria;
}


    // POST: api/empresas
    [HttpPost]
    public async Task<ActionResult<Empresa>> CrearEmpresa(CreateEmpresaDto dto)
    {
        var empresa = await _service.CrearEmpresaAsync(dto);

        await _auditoria.RegistrarEventoAsync(
            accion: "Crear Empresa",
            entidad: "Empresa",
            entidadId: empresa.Id,
            descripcion: $"Se creó la empresa '{empresa.Nombre}'"
        );

        return CreatedAtAction(nameof(GetEmpresaPorId), new { id = empresa.Id }, empresa);
    }

    // GET: api/empresas
    [HttpGet]
    public async Task<ActionResult<List<Empresa>>> GetEmpresas()
    {
        var empresas = await _service.ObtenerTodasAsync();
        return Ok(empresas);
    }

    // GET: api/empresas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Empresa>> GetEmpresaPorId(int id)
    {
        var empresa = await _service.ObtenerPorIdAsync(id);
        if (empresa == null) return NotFound();
        return Ok(empresa);
    }

    // PUT: api/empresas/5
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarEmpresa(int id, UpdateEmpresaDto dto)
    {
        var empresaAntesDeActualizar = await _service.ObtenerPorIdAsync(id);
        if (empresaAntesDeActualizar == null) return NotFound();

        var actualizado = await _service.ActualizarEmpresaAsync(id, dto);
        if (!actualizado) return NotFound();

        await _auditoria.RegistrarEventoAsync(
            accion: "Editar Empresa",
            entidad: "Empresa",
            entidadId: id,
            descripcion: $"Se actualizó la empresa '{empresaAntesDeActualizar.Nombre}'"
        );

        return NoContent();
    }

    // DELETE: api/empresas/5
   [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarEmpresa(int id)
    {
        var empresaAntesDeEliminar = await _service.ObtenerPorIdAsync(id);
        if (empresaAntesDeEliminar == null) return NotFound();

        var eliminado = await _service.EliminarEmpresaAsync(id);
        if (!eliminado) return NotFound();

        await _auditoria.RegistrarEventoAsync(
            accion: "Eliminar Empresa",
            entidad: "Empresa",
            entidadId: id,
            descripcion: $"Se eliminó la empresa '{empresaAntesDeEliminar.Nombre}'"
        );

        return NoContent();
    }

    // obtener el total de las empresas 
    [HttpGet("total")]
    public async Task<ActionResult<int>> GetTotalEmpresas()
    {
        int total = await _service.ContarEmpresasAsync();
        return Ok(total);
    }



}
