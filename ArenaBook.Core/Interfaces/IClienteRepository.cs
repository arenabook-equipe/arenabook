using System.Threading;
using System.Threading.Tasks;
using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;

namespace ArenaBook.Core.Interfaces;

public interface IClienteRepository
{
    Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken);

    Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken);
    Task<Cliente?> ObterPorIdAsync(int idCliente, CancellationToken cancellationToken);
    Task<bool> AtualizarAsync(
    int idCliente,
    string nome,
    string telefone,
    CancellationToken cancellationToken);
    Task<ResultadoExclusaoCliente> ExcluirAsync(
    int idCliente,
    CancellationToken cancellationToken);

}