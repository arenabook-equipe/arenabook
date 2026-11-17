using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly ArenaBookDbContext _context;

    public ClienteRepository(ArenaBookDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistePorEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return _context.Clientes.AnyAsync(
            cliente => cliente.Email == email,
            cancellationToken);
    }

    public async Task AdicionarAsync(
        Cliente cliente,
        CancellationToken cancellationToken)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);
    }
    public Task<Cliente?> ObterPorIdAsync(
    int idCliente,
    CancellationToken cancellationToken)
    {
        return _context.Clientes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                cliente => cliente.IdCliente == idCliente,
                cancellationToken);
    }
}