using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ArenaBook.Core.Enums;

namespace ArenaBook.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly ArenaBookDbContext _context;
    public ClienteRepository(ArenaBookDbContext context)
    {
        _context = context;
    }
    public Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _context.Clientes.AnyAsync(cliente => cliente.Email == email, cancellationToken);
    }
    public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);
    }
    public Task<Cliente?> ObterPorIdAsync(int idCliente, CancellationToken cancellationToken)
    {
        return _context.Clientes.AsNoTracking().SingleOrDefaultAsync(cliente => cliente.IdCliente == idCliente, cancellationToken);
    }
    public async Task<bool> AtualizarAsync(int idCliente, string nome, string telefone, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.SingleOrDefaultAsync(cliente => cliente.IdCliente == idCliente, cancellationToken);
        if (cliente is null)
        {
            return false;
        }

        cliente.Nome = nome;
        cliente.Telefone = telefone;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
    public async Task<ResultadoExclusaoCliente> ExcluirAsync(int idCliente, CancellationToken cancellationToken)
    {
        var clientes = _context.Clientes.Where(cliente => cliente.IdCliente == idCliente);

        var linhasExcluidas = await clientes.Where(cliente => !cliente.Reservas.Any()).ExecuteDeleteAsync(cancellationToken);

        if (linhasExcluidas > 0)
        {
            return ResultadoExclusaoCliente.Excluido;
        }

        var clienteExiste = await clientes.AnyAsync(cancellationToken);

        return clienteExiste ? ResultadoExclusaoCliente.PossuiReservas : ResultadoExclusaoCliente.NaoEncontrado;
    }
}