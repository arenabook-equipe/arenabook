using ArenaBook.Core.Entities;

namespace ArenaBook.Core.Interfaces;

public interface IHorarioFuncionamentoRepository
{
    Task<IReadOnlyList<HorarioFuncionamento>> ConsultarAsync(int idQuadra, CancellationToken cancellationToken = default);
    Task<HorarioFuncionamento?> ObterPorIdAsync(int idQuadra, int idHorario, CancellationToken cancellationToken = default);
    Task AdicionarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default);
    Task<bool> AtualizarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default);
}
