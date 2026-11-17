using System.Threading;
using System.Threading.Tasks;
using ArenaBook.Core.Entities;

namespace ArenaBook.Core.Interfaces;

public interface IClienteRepository
{
    Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken);

    Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken);
    Task<Cliente?> ObterPorIdAsync(int idCliente, CancellationToken cancellationToken);

}