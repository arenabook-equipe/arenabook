using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class QuadraRepository : IQuadraRepository
{
    private readonly ArenaBookDbContext _context;

    public QuadraRepository(ArenaBookDbContext context)
    {
        _context = context;
    }

    //busca a quadra pra validação e reserva
    public Task<Quadra?> ObterParaReservaAsync(int idQuadra, CancellationToken cancellationToken = default)
    {
        return _context.Quadras.AsNoTracking()
            .Include(quadra => quadra.QuadraEsportes)
            .Include(quadra => quadra.HorariosFuncionamento)
            .AsSplitQuery()
            .SingleOrDefaultAsync(quadra => quadra.IdQuadra == idQuadra, cancellationToken);
    }
}