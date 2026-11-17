using ArenaBook.Core.Entities;

namespace ArenaBook.Core.Interfaces;

public interface IQuadraRepository
{
    //verificará se a quadra existe
    Task<Quadra?> ObterParaReservaAsync(
        int idQuadra,
        CancellationToken cancellationToken = default);
}