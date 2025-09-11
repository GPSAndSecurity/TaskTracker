using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.DTOs;
using TaskTracker.Models;
using TaskTracker.Services;

[Route("api/[controller]")]
[ApiController]
[Authorize]


public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;
    private readonly AuditoriaService _auditoria;

public ClientesController(ClienteService service, AuditoriaService auditoria)
{
    _service = service;
    _auditoria = auditoria;
}


    // GET: api/clientes
    [HttpGet]
    [Authorize(Roles = "admin_empresa,superadmin, colaborador")]
    public async Task<ActionResult<List<Cliente>>> GetClientes()
    {
        int empresaId = ObtenerEmpresaIdDesdeToken();
        var clientes = await _service.ObtenerClientesPorEmpresaAsync(empresaId);
        return Ok(clientes);
    }

    // GET: api/clientes/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "admin_empresa,superadmin , colaborador")]
    public async Task<ActionResult<Cliente>> GetClientePorId(int id)
    {
        var cliente = await _service.ObtenerClientePorIdAsync(id);
        if (cliente == null || cliente.EmpresaId != ObtenerEmpresaIdDesdeToken())
            return Forbid();

        return Ok(cliente);
    }

    // POST: api/clientes
   [HttpPost]
[Authorize(Roles = "admin_empresa,superadmin")]
public async Task<ActionResult<Cliente>> CrearCliente(CreateClienteDto dto)
{
    dto.EmpresaId = ObtenerEmpresaIdDesdeToken(); 
    var cliente = await _service.CrearClienteAsync(dto);

    await _auditoria.RegistrarEventoAsync(
        accion: "Crear Cliente",
        entidad: "Cliente",
        entidadId: cliente.Id,
        descripcion: $"Se creó el cliente '{cliente.Nombre}'",
        generaNotificacion: true
    );

    return CreatedAtAction(nameof(GetClientePorId), new { id = cliente.Id }, cliente);
}


    // PUT: api/clientes/{id}
    [HttpPut("{id}")]
[Authorize(Roles = "admin_empresa,superadmin")]
public async Task<IActionResult> ActualizarCliente(int id, UpdateClienteDto dto)
{
    var cliente = await _service.ObtenerClientePorIdAsync(id);
    if (cliente == null || cliente.EmpresaId != ObtenerEmpresaIdDesdeToken())
        return Forbid();

    var actualizado = await _service.ActualizarClienteAsync(id, dto);
    if (!actualizado) return NotFound();

    await _auditoria.RegistrarEventoAsync(
        accion: "Editar Cliente",
        entidad: "Cliente",
        entidadId: id,
        descripcion: $"Se actualizó el cliente '{cliente.Nombre}'"
    );

    return NoContent();
}


    // DELETE: api/clientes/{id}
    [HttpDelete("{id}")]
[Authorize(Roles = "admin_empresa,superadmin")]
public async Task<IActionResult> EliminarCliente(int id)
{
    var cliente = await _service.ObtenerClientePorIdAsync(id);
    if (cliente == null || cliente.EmpresaId != ObtenerEmpresaIdDesdeToken())
        return Forbid();

    var eliminado = await _service.EliminarClienteAsync(id);
    if (!eliminado) return NotFound();

    await _auditoria.RegistrarEventoAsync(
        accion: "Eliminar Cliente",
        entidad: "Cliente",
        entidadId: id,
        descripcion: $"Se eliminó el cliente '{cliente.Nombre}'",
         generaNotificacion: true
    );

    return NoContent();
}


    //  total de clientes por empresa
    [HttpGet("total")]
    [Authorize(Roles = "admin_empresa,superadmin , colaborador")]
    public async Task<ActionResult<int>> GetTotalClientes()
    {
        int empresaId = ObtenerEmpresaIdDesdeToken();
        int total = await _service.ContarClientesPorEmpresaAsync(empresaId);
        return Ok(total);
    }

    private int ObtenerEmpresaIdDesdeToken()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "empresaId")?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int empresaId))
            throw new UnauthorizedAccessException("empresaId inválido en el token");

        return empresaId;
    }


}
