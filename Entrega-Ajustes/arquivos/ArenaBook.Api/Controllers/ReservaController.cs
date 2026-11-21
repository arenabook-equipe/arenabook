using System.ComponentModel.DataAnnotations;
using ArenaBook.Api.Contracts;
using ArenaBook.Core.Exceptions;
using ArenaBook.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/reservas")]
public class ReservaController(IReservaService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReservaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarReservaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var reserva = await service.RegistrarAsync(
                request.IdCliente,
                request.IdQuadra,
                request.IdEsporte,
                request.DataReserva,
                request.HoraInicio,
                request.HoraFim,
                cancellationToken);

            var response = ReservaResponse.De(reserva);

            return Created(
                $"api/reservas/{response.IdReserva}",
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ConflitoReservaException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }

    [HttpGet("agenda")]
    [ProducesResponseType(typeof(ReservaResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Agenda(
        [FromQuery, Range(1, int.MaxValue)] int idQuadra,
        [FromQuery, Required] DateOnly? data,
        CancellationToken cancellationToken)
    {
        try
        {
            var reservas = await service.ConsultarAgendaAsync(
                idQuadra,
                data!.Value,
                cancellationToken);

            return Ok(reservas.Select(ReservaResponse.De).ToArray());
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

    [HttpPatch("{id:int}/cancelar")]
    [ProducesResponseType(typeof(ReservaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var reserva = await service.CancelarAsync(
                id, cancellationToken);

            return Ok(ReservaResponse.De(reserva));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ConflitoReservaException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }
}