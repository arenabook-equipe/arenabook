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
            var response = QuadraResponse.CriarResposta(quadra);
            var rota = new { id = quadra.IdQuadra };

            return CreatedAtAction(nameof(Consultar), rota, response);
        }
        catch (QuadraInvalidaException ex)
        {
            var erro = new { erro = ex.Message };

            return BadRequest(erro);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Consultar(int id)
    {
        try
        {
            var quadra = await service.ConsultarAsync(id);

            var response = QuadraResponse.CriarResposta(quadra);

            return Ok(response);
        }
        catch (QuadraNaoEncontradaException ex)
        {
            var erro = new { erro = ex.Message };

            return NotFound(erro);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarQuadraRequest request)
    {
        try
        {
            var quadra = await service.EditarAsync(id, request.Nome, request.IdsEsportes, request.PrecoHora);
            var response = QuadraResponse.CriarResposta(quadra);

            return Ok(response);
        }
        catch (QuadraInvalidaException ex)
        {
            var erro = new { erro = ex.Message };

            return BadRequest(erro);
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
            var erro = new { erro = ex.Message };

            return NotFound(erro);
        }
    }

    [HttpGet("{id}/disponibilidade")]
    public async Task<IActionResult> Disponibilidade(int id)
    {
        try
        {
            var disponivel = await service.EstaDisponivelAsync(id);

            var response = new { idQuadra = id, disponivel };

            return Ok(response);
        }
        catch (QuadraNaoEncontradaException ex)
        {
            var erro = new { erro = ex.Message };

            return NotFound(erro);
        }
    }
}