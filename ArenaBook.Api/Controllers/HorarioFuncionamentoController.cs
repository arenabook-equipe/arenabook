using ArenaBook.Api.Contracts.Requests;
using ArenaBook.Api.Contracts.Responses;
using ArenaBook.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/quadras/{idQuadra:int}/funcionamento")]
public class HorarioFuncionamentoController : ControllerBase
{
    private readonly HorarioFuncionamentoService _service;

    public HorarioFuncionamentoController(HorarioFuncionamentoService service)
    {
        _service = service;
    }

    [HttpPost]
    [EndpointSummary("Cadastra o funcionamento de uma quadra")]
    [EndpointDescription("Informe o dia da semana de 1 (domingo) a 7 (sábado), a abertura e o fechamento. Exemplo: diaSemana 2, horaAbertura 08:00:00 e horaFechamento 22:00:00.")]
    public async Task<IActionResult> Cadastrar(int idQuadra, [FromBody] SalvarFuncionamentoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var horario = await _service.CadastrarAsync(idQuadra, request.DiaSemana, request.HoraAbertura!.Value, request.HoraFechamento!.Value, cancellationToken);

            var response = HorarioFuncionamentoResponse.CriarResposta(horario);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao cadastrar funcionamento." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }

    [HttpGet]
    [EndpointSummary("Consulta o funcionamento de uma quadra")]
    [EndpointDescription("Retorna os horários cadastrados para a quadra, ordenados por dia da semana e abertura. Sem configuração, retorna uma lista vazia.")]
    public async Task<IActionResult> Consultar(int idQuadra, CancellationToken cancellationToken)
    {
        try
        {
            var horarios = await _service.ConsultarAsync(idQuadra, cancellationToken);

            var response = horarios.Select(HorarioFuncionamentoResponse.CriarResposta).ToArray();

            return Ok(response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao consultar funcionamento." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }

    [HttpPut("{idHorario:int}")]
    [EndpointSummary("Altera o funcionamento de uma quadra")]
    [EndpointDescription("Altera o dia, a abertura e o fechamento da configuração indicada. Se os dados forem inválidos, a configuração anterior é preservada.")]
    public async Task<IActionResult> Alterar(int idQuadra, int idHorario, [FromBody] SalvarFuncionamentoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var horario = await _service.AlterarAsync(idQuadra, idHorario, request.DiaSemana, request.HoraAbertura!.Value, request.HoraFechamento!.Value, cancellationToken);

            var response = HorarioFuncionamentoResponse.CriarResposta(horario);

            return Ok(response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao alterar funcionamento." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }
}
