using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;

namespace ArenaBook.Core.Services;

public class QuadraService(IQuadraCadastroRepository repositorio) : IQuadraDisponibilidade
{
    public async Task<Quadra> CadastrarAsync(string? nome, IEnumerable<int>? idsEsportes, decimal precoHora)
    {
        var esportes = (idsEsportes ?? Array.Empty<int>()).Distinct().ToList();
        Validar(nome, esportes, precoHora);

        var quadra = new Quadra
        {
            Nome = nome!.Trim(),
            PrecoHora = precoHora,
            QuadraEsportes = esportes.Select(id => new QuadraEsporte { IdEsporte = id }).ToList()
        };

        return await repositorio.AdicionarAsync(quadra);
    }

    public async Task<Quadra> ConsultarAsync(int idQuadra)
    {
        return await repositorio.ObterPorIdAsync(idQuadra)
            ?? throw new QuadraNaoEncontradaException($"Quadra {idQuadra} não encontrada");
    }

    public async Task<Quadra> EditarAsync(int idQuadra, string? nome, IEnumerable<int>? idsEsportes, decimal? precoHora)
    {
        var quadra = await ConsultarAsync(idQuadra);

        var novoNome = nome ?? quadra.Nome;
        var novosEsportes = idsEsportes?.Distinct().ToList()
            ?? quadra.QuadraEsportes.Select(qe => qe.IdEsporte).ToList();
        var novoPreco = precoHora ?? quadra.PrecoHora;

        Validar(novoNome, novosEsportes, novoPreco);

        var remover = idsEsportes == null
            ? new List<QuadraEsporte>()
            : quadra.QuadraEsportes.Where(qe => !novosEsportes.Contains(qe.IdEsporte)).ToList();

        if (remover.Any(qe => qe.Reservas.Count > 0))
            throw new QuadraInvalidaException("Não é possível remover uma modalidade que já possui reservas");

        quadra.Nome = novoNome!.Trim();
        quadra.PrecoHora = novoPreco;

        if (idsEsportes != null)
        {
            foreach (var qe in remover)
                quadra.QuadraEsportes.Remove(qe);

            var existentes = quadra.QuadraEsportes.Select(qe => qe.IdEsporte).ToList();
            foreach (var idEsporte in novosEsportes.Except(existentes))
                quadra.QuadraEsportes.Add(new QuadraEsporte { IdQuadra = quadra.IdQuadra, IdEsporte = idEsporte });
        }

        await repositorio.AtualizarAsync(quadra);
        return quadra;
    }

    public async Task DesativarAsync(int idQuadra)
    {
        var quadra = await ConsultarAsync(idQuadra);
        quadra.Ativa = false;
        await repositorio.AtualizarAsync(quadra);
    }

    public async Task<bool> EstaDisponivelAsync(int idQuadra)
    {
        return (await ConsultarAsync(idQuadra)).Ativa;
    }

    private static void Validar(string? nome, IReadOnlyCollection<int> idsEsportes, decimal precoHora)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new QuadraInvalidaException("Nome da quadra é obrigatório");
        if (idsEsportes.Count == 0)
            throw new QuadraInvalidaException("Informe ao menos uma modalidade");
        if (precoHora <= 0)
            throw new QuadraInvalidaException("Preço por hora deve ser maior que zero");
    }
}