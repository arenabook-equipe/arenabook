using ArenaBook.Core.Entities;

namespace ArenaBook.Api.Dtos;

public record CadastrarQuadraRequest(string? Nome, List<int>? IdsEsportes, decimal PrecoHora);

public record EditarQuadraRequest(string? Nome, List<int>? IdsEsportes, decimal? PrecoHora);

public record QuadraResponse(int IdQuadra, string? Nome, decimal PrecoHora, bool Ativa, List<int> IdsEsportes)
{
    public static QuadraResponse CriarResposta(Quadra q) =>
        new(q.IdQuadra, q.Nome, q.PrecoHora, q.Ativa, q.QuadraEsportes.Select(qe => qe.IdEsporte).ToList());
}