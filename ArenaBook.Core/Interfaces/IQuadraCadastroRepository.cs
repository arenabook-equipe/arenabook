using ArenaBook.Core.Entities;

namespace ArenaBook.Core.Interfaces;

public interface IQuadraCadastroRepository
{
    Task<Quadra> AdicionarAsync(Quadra quadra);
    Task<Quadra?> ObterPorIdAsync(int idQuadra);
    Task AtualizarAsync(Quadra quadra);
}