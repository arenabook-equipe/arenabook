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
        var reserva = await _service.RegistrarAsync(
            idCliente: request.IdCliente,
            idQuadra: request.IdQuadra,
            idEsporte: request.IdEsporte,
            dataReserva: request.DataReserva,
            horaInicio: request.HoraInicio,
            horaFim: request.HoraFim);

        var response = ReservaResponse.De(reserva);
        return Created($"api/reservas/{response.IdReserva}", response);
    }

    [HttpPatch("{id}/cancelar")]
    [ProducesResponseType(typeof(ReservaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var reserva = await _service.CancelarAsync(id);
        return Ok(ReservaResponse.De(reserva));
    }
}