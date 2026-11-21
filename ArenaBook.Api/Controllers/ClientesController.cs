using ArenaBook.Api.DTOs;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArenaBook.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _clienteService;
    public ClientesController(ClienteService clienteService)
    {
        _clienteService = clienteService;
    }
    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CriarClienteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cliente = await _clienteService.CadastrarAsync(request.Nome, request.Telefone, request.Email, cancellationToken);
            if (cliente is null)
            {
                var erro = new { mensagem = "Este e-mail já está cadastrado." };

                return Conflict(erro);
            }
            var response = new { cliente.IdCliente, cliente.Nome, cliente.Telefone, cliente.Email };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            var erro = new { mensagem = "Erro ao cadastrar cliente." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }
   
 
    [HttpPut("{idCliente:int}")]
    public async Task<IActionResult> Atualizar(int idCliente, [FromBody] AtualizarClienteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var atualizado = await _clienteService.AtualizarAsync(idCliente, request.Nome, request.Telefone, cancellationToken);
            if (!atualizado)
            {
                var erro = new { mensagem = "Cliente não encontrado." };

                return NotFound(erro);
            }
            return NoContent();
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao atualizar cliente." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }
    [HttpDelete("{idCliente:int}")]
    public async Task<IActionResult> Excluir(int idCliente, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _clienteService.ExcluirAsync(idCliente, cancellationToken);
            return resultado switch
            {
                ResultadoExclusaoCliente.Excluido => NoContent(),
                ResultadoExclusaoCliente.NaoEncontrado => NotFound(new { mensagem = "Cliente não encontrado." }),
                ResultadoExclusaoCliente.PossuiReservas => Conflict(new { mensagem = "O cliente possui reservas vinculadas." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao excluir cliente." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }
    [HttpGet("{idCliente:int}")]
    public async Task<IActionResult> ObterPorId(int idCliente, CancellationToken cancellationToken)
    {
        try
        {
            var cliente = await _clienteService.ObterPorIdAsync(idCliente, cancellationToken);
            if (cliente is null)
            {
                var erro = new { mensagem = "Cliente não encontrado." };

                return NotFound(erro);
            }
            var response = new { cliente.IdCliente, cliente.Nome, cliente.Telefone, cliente.Email };

            return Ok(response);
        }
        catch (Exception)
        {
            var erro = new { mensagem = "Erro ao consultar cliente." };

            return StatusCode(StatusCodes.Status500InternalServerError, erro);
        }
    }
}