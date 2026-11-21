using ArenaBook.Api.DTOs;
using ArenaBook.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/quadras/{idQuadra:int}/funcionamento")]
public class HorarioFuncionamentoController(
    HorarioFuncionamentoService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(HorarioFuncionamentoResponse[]),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(
        int idQuadra,
        CancellationToken cancellationToken)
    {
        try
        {
            var horarios = await service.ConsultarAsync(
                idQuadra, cancellationToken);

            return Ok(horarios
                .Select(HorarioFuncionamentoResponse.De)
                .ToArray());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(HorarioFuncionamentoResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cadastrar(
        int idQuadra,
        [FromBody] SalvarFuncionamentoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var horario = await service.CadastrarAsync(
                idQuadra,
                request.DiaSemana,
                request.HoraAbertura!.Value,
                request.HoraFechamento!.Value,
                cancellationToken);

            return CreatedAtAction(
                nameof(Consultar),
                new { idQuadra },
                HorarioFuncionamentoResponse.De(horario));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{idHorario:int}")]
    [ProducesResponseType(
        typeof(HorarioFuncionamentoResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(
        int idQuadra,
        int idHorario,
        [FromBody] SalvarFuncionamentoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var horario = await service.AlterarAsync(
                idQuadra,
                idHorario,
                request.DiaSemana,
                request.HoraAbertura!.Value,
                request.HoraFechamento!.Value,
                cancellationToken);

            return Ok(HorarioFuncionamentoResponse.De(horario));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
