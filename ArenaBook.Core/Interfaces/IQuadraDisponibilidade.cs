namespace ArenaBook.Core.Interfaces;

public interface IQuadraDisponibilidade
{
    Task<bool> EstaDisponivelAsync(int idQuadra);
}