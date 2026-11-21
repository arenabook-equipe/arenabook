using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class HorarioFuncionamentoRepository(ArenaBookDbContext context)
    : IHorarioFuncionamentoRepository
{
    public async Task<IReadOnlyList<HorarioFuncionamento>> ConsultarAsync(int idQuadra, CancellationToken cancellationToken = default)
    {
        return await context.HorariosFuncionamento
            .AsNoTracking()
            .Where(h => h.IdQuadra == idQuadra)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraAbertura)
            .ToListAsync(cancellationToken);
    }

    public Task<HorarioFuncionamento?> ObterAsync(int idQuadra, int idHorario, CancellationToken cancellationToken = default)
    {
        return context.HorariosFuncionamento
            .SingleOrDefaultAsync(
                h => h.IdQuadra == idQuadra &&
                     h.IdHorario == idHorario,
                cancellationToken);
    }

    public async Task AdicionarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default)
    {
        context.HorariosFuncionamento.Add(horario);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
