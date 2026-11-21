using ArenaBook.Api.Contracts.Requests;
using ArenaBook.Api.Contracts.Responses;
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
    [EndpointSummary("Registra uma reserva")]
    [EndpointDescription("Calcula o valor e registra como Pendente. Exige cliente cadastrado, quadra ativa, modalidade compatível e intervalo disponível dentro do funcionamento.")]
    public async Task<IActionResult> Criar([FromBody] CriarReservaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var reserva = await _service.RegistrarAsync(request.IdCliente, request.IdQuadra, request.IdEsporte, request.DataReserva!.Value, request.HoraInicio!.Value, request.HoraFim!.Value, cancellationToken);

            var response = ReservaResponse.CriarResposta(reserva);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao registrar reserva." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }

    [HttpGet("agenda")]
    [EndpointSummary("Consulta a agenda de uma quadra")]
    [EndpointDescription("Informe idQuadra e data no formato AAAA-MM-DD. Retorna reservas Pendente e Confirmada, ordenadas pelo início.")]
    public async Task<IActionResult> Agenda(int idQuadra, DateOnly data, CancellationToken cancellationToken)
    {
        try
        {
            var reservas = await _service.ConsultarAgendaAsync(idQuadra, data, cancellationToken);

            var response = reservas.Select(ReservaResponse.CriarResposta).ToArray();

            return Ok(response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao consultar agenda." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }

    [HttpPatch("{id}/cancelar")]
    [EndpointSummary("Cancela uma reserva ativa")]
    [EndpointDescription("Altera o status para Cancelada e preserva o histórico. A reserva deixa de ocupar o horário na agenda.")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        try
        {
            var reserva = await _service.CancelarAsync(id, cancellationToken);

            var response = ReservaResponse.CriarResposta(reserva);

            return Ok(response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao cancelar reserva." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }
}
