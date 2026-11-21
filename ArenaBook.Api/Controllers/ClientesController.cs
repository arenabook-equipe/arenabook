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
                return Conflict(new { mensagem = "Este e-mail já está cadastrado." });
            }
            return StatusCode(StatusCodes.Status201Created, new { cliente.IdCliente, cliente.Nome, cliente.Telefone, cliente.Email });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensagem = "Erro ao cadastrar cliente." });
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
                return NotFound(new { mensagem = "Cliente não encontrado." });
            }
            return NoContent();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensagem = "Erro ao atualizar cliente." });
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensagem = "Erro ao excluir cliente." });
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
                return NotFound(new { mensagem = "Cliente não encontrado." });
            }
            return Ok(new { cliente.IdCliente, cliente.Nome, cliente.Telefone, cliente.Email });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensagem = "Erro ao consultar cliente." });
        }
    }
}