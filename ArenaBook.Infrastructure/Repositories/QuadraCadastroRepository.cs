using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class QuadraCadastroRepository : IQuadraCadastroRepository
{
    private readonly ArenaBookDbContext _context;

    public QuadraCadastroRepository(ArenaBookDbContext context)
    {
        _context = context;
    }

    public async Task<Quadra> AdicionarAsync(Quadra quadra)
    {
        _context.Quadras.Add(quadra);
        await _context.SaveChangesAsync();
        return quadra;
    }

    public async Task<Quadra?> ObterPorIdAsync(int idQuadra)
    {
        return await _context.Quadras
            .Include(q => q.QuadraEsportes)
                .ThenInclude(qe => qe.Reservas)
            .FirstOrDefaultAsync(q => q.IdQuadra == idQuadra);
    }

    public async Task AtualizarAsync(Quadra quadra)
    {
        // A quadra veio do próprio contexto (rastreada), então basta salvar.
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TodosEsportesExistemAsync(IReadOnlyCollection<int> idsEsportes)
    {
        var encontrados = await _context.Esportes.CountAsync(e => idsEsportes.Contains(e.IdEsporte));
        return encontrados == idsEsportes.Count;
    }
}