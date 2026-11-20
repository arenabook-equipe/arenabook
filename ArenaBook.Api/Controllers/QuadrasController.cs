using ArenaBook.Api.Dtos;
using ArenaBook.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuadrasController(QuadraService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarQuadraRequest request)
    {
        try
        {
            var quadra = await service.CadastrarAsync(request.Nome, request.IdsEsportes, request.PrecoHora);
            return CreatedAtAction(nameof(Consultar), new { id = quadra.IdQuadra }, QuadraResponse.De(quadra));
        }
        catch (QuadraInvalidaException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Consultar(int id)
    {
        try
        {
            return Ok(QuadraResponse.De(await service.ConsultarAsync(id)));
        }
        catch (QuadraNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarQuadraRequest request)
    {
        try
        {
            var quadra = await service.EditarAsync(id, request.Nome, request.IdsEsportes, request.PrecoHora);
            return Ok(QuadraResponse.De(quadra));
        }
        catch (QuadraNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (QuadraInvalidaException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> Desativar(int id)
    {
        try
        {
            await service.DesativarAsync(id);
            return NoContent();
        }
        catch (QuadraNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("{id}/disponibilidade")]
    public async Task<IActionResult> Disponibilidade(int id)
    {
        try
        {
            return Ok(new { idQuadra = id, disponivel = await service.EstaDisponivelAsync(id) });
        }
        catch (QuadraNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }
}