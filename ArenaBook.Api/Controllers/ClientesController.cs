using System.Threading;
using System.Threading.Tasks;
using ArenaBook.Api.DTOs;
using ArenaBook.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
}