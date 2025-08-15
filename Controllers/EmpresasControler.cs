using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;


[Route("api/[controller]")]
[ApiController]
public class EmpresasController : ControllerBase
{
    private readonly EmpresaService _service;

    public EmpresasController(EmpresaService service)
    {
        _service = service;
    }

    // POST: api/empresas
    [HttpPost]
    public async Task<ActionResult<Empresa>> CrearEmpresa(CreateEmpresaDto dto)
    {
        var empresa = await _service.CrearEmpresaAsync(dto);
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
        var actualizado = await _service.ActualizarEmpresaAsync(id, dto);
        if (!actualizado) return NotFound();
        return NoContent();
    }

    // DELETE: api/empresas/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarEmpresa(int id)
    {
        var eliminado = await _service.EliminarEmpresaAsync(id);
        if (!eliminado) return NotFound();
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
