    using System.Threading;
using System.Threading.Tasks;
using ArenaBook.Api.DTOs;
using ArenaBook.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArenaBook.Core.Enums;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        [FromBody] CriarClienteRequest request,
        CancellationToken cancellationToken)
    {
        var cliente = await _service.CadastrarAsync(
            request.Nome,
            request.Telefone,
            request.Email,
            cancellationToken);

        if (cliente is null)
        {
            return Conflict(new { mensagem = "Este e-mail já está cadastrado." });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            cliente.IdCliente,
            cliente.Nome,
            cliente.Telefone,
            cliente.Email
        });
    }
    [HttpGet("{idCliente:int}")]
    public async Task<IActionResult> ConsultarPorId(
    int idCliente,
    CancellationToken cancellationToken)
    {
        var cliente = await _service.ObterPorIdAsync(idCliente, cancellationToken);

        if (cliente is null)
        {
            return NotFound(new { mensagem = "Cliente não encontrado." });
        }

        return Ok(new
        {
            cliente.IdCliente,
            cliente.Nome,
            cliente.Telefone,
            cliente.Email
        });
    }
    [HttpPut("{idCliente:int}")]
    public async Task<IActionResult> Atualizar(
    int idCliente,
    [FromBody] AtualizarClienteRequest request,
    CancellationToken cancellationToken)
    {
        var atualizado = await _service.AtualizarAsync(
            idCliente,
            request.Nome,
            request.Telefone,
            cancellationToken);

        if (!atualizado)
        {
            return NotFound(new { mensagem = "Cliente não encontrado." });
        }

        return NoContent();
    }
    [HttpDelete("{idCliente:int}")]
    public async Task<IActionResult> Excluir(
    int idCliente,
    CancellationToken cancellationToken)
    {
        var resultado = await _service.ExcluirAsync(idCliente, cancellationToken);

        return resultado switch
        {
            ResultadoExclusaoCliente.Excluido => NoContent(),
            ResultadoExclusaoCliente.NaoEncontrado => NotFound(
                new { mensagem = "Cliente não encontrado." }),
            ResultadoExclusaoCliente.PossuiReservas => Conflict(
                new { mensagem = "O cliente possui reservas vinculadas." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}