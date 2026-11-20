using ArenaBook.Api.Contracts;
using ArenaBook.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/reservas")]
public class ReservaController : ControllerBase
{
    private readonly IReservaService _service;
    public ReservaController(IReservaService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(ReservaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(CriarReservaRequest request)
    {
        var reserva = await _service.CriarAsync(request.IdQuadra, request.IdEsporte,
            request.IdCliente, request.DataReserva, request.HoraInicio, request.HoraFim);
        var response = ReservaResponse.De(reserva);
        return Created($"api/reservas/{response.IdReserva}", response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Agenda([FromQuery] int idQuadra, [FromQuery] DateOnly data)
    {
        var reservas = await _service.ObterAgendaAsync(idQuadra, data);
        return Ok(reservas.Select(ReservaResponse.De));
    }

    [HttpPatch("{id}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id)
    {
        await _service.CancelarAsync(id);
        return NoContent();
    }
}
