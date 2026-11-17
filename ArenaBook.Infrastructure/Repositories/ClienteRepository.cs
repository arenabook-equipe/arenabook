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

    //busca o cliente pra validação
    public Task<bool> ExisteAsync(int idCliente, CancellationToken cancellationToken = default) 
    {
        return _context.Clientes.AnyAsync(cliente => cliente.IdCliente == idCliente, cancellationToken);
    }
}