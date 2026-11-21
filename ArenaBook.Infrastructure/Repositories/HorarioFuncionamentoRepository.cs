using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class HorarioFuncionamentoRepository : IHorarioFuncionamentoRepository
{
    private readonly ArenaBookDbContext _context;

    public HorarioFuncionamentoRepository(ArenaBookDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HorarioFuncionamento>> ConsultarAsync(int idQuadra, CancellationToken cancellationToken = default)
    {
        return await _context.HorariosFuncionamento.AsNoTracking()
            .Where(horario => horario.IdQuadra == idQuadra)
            .OrderBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraAbertura)
            .ToListAsync(cancellationToken);
    }

    public Task<HorarioFuncionamento?> ObterPorIdAsync(int idQuadra, int idHorario, CancellationToken cancellationToken = default)
    {
        return _context.HorariosFuncionamento.AsNoTracking().SingleOrDefaultAsync(horario => horario.IdQuadra == idQuadra && horario.IdHorario == idHorario, cancellationToken);
    }

    public async Task AdicionarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default)
    {
        _context.HorariosFuncionamento.Add(horario);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AtualizarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default)
    {
        //atualiza sem modificar uma chave alternativa em uma entidade rastreada.
        var quantidadeAtualizada = await _context.HorariosFuncionamento.Where(item => item.IdQuadra == horario.IdQuadra && item.IdHorario == horario.IdHorario)
            .ExecuteUpdateAsync(atualizacao => atualizacao
                .SetProperty(item => item.DiaSemana, horario.DiaSemana)
                .SetProperty(item => item.HoraAbertura, horario.HoraAbertura)
                .SetProperty(item => item.HoraFechamento, horario.HoraFechamento), cancellationToken);

        return quantidadeAtualizada == 1;
    }
}
