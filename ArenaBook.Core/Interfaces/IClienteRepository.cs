namespace ArenaBook.Core.Interfaces;

public interface IClienteRepository
{
    //verificará se o cliente existe
    Task<bool> ExisteAsync(
        int idCliente,
        CancellationToken cancellationToken = default);
}