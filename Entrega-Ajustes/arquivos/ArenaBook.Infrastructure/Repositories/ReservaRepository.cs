using System.Data;
using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class ReservaRepository : IReservaRepository
{
    private readonly ArenaBookDbContext _context;

    public ReservaRepository(ArenaBookDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteSobreposicaoAsync(int idQuadra, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default)
    {
        return _context.Reservas.AnyAsync(reserva =>
                reserva.IdQuadra == idQuadra &&
                reserva.DataReserva == dataReserva &&
                statusBloqueantes.Contains(reserva.IdStatus) &&
                reserva.HoraInicio < horaFim &&
                reserva.HoraFim > horaInicio, cancellationToken);
    }
    public async Task<bool> TentarRegistrarSemSobreposicaoAsync(Reserva reserva, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        // Bloqueia a quadra até o fim da transação para evitar reservas simultâneas conflitantes.
        await _context.Quadras.FromSqlInterpolated(
            $"""
                SELECT IdQuadra, Nome, PrecoHora, Ativa
                FROM Quadra WITH (UPDLOCK, HOLDLOCK)
                WHERE IdQuadra = {reserva.IdQuadra}
                """).AsNoTracking()
            .SingleAsync(cancellationToken);

        var existeSobreposicao = await ExisteSobreposicaoAsync(reserva.IdQuadra, reserva.DataReserva, reserva.HoraInicio, reserva.HoraFim, statusBloqueantes, cancellationToken);

        if (existeSobreposicao)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        _context.Reservas.Add(reserva);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
    //consultará a reserva pelo id.
    public Task<Reserva?> ObterPorIdAsync(int idReserva, CancellationToken cancellationToken = default)
    {
        return _context.Reservas.AsNoTracking().SingleOrDefaultAsync(reserva => reserva.IdReserva == idReserva, cancellationToken);
    }
    //consultará a reserva pelo id e status.
    public async Task<bool> TentarCancelarAsync(int idReserva, IReadOnlyCollection<StatusReserva> statusPermitidos, CancellationToken cancellationToken = default)
    {
        var quantidadeAtualizada = await _context.Reservas.Where(reserva => reserva.IdReserva == idReserva && statusPermitidos.Contains(reserva.IdStatus))
            .ExecuteUpdateAsync(atualizacao => atualizacao.SetProperty(reserva => reserva.IdStatus, StatusReserva.Cancelada), cancellationToken);

        return quantidadeAtualizada == 1;
    }

    public async Task<IReadOnlyList<Reserva>> ConsultarAgendaAsync(int idQuadra, DateOnly dataReserva, CancellationToken cancellationToken = default)
    {
        return await _context.Reservas
            .AsNoTracking()
            .Where(reserva =>
                reserva.IdQuadra == idQuadra &&
                reserva.DataReserva == dataReserva &&
                (reserva.IdStatus == StatusReserva.Pendente ||
                 reserva.IdStatus == StatusReserva.Confirmada))
            .OrderBy(reserva => reserva.HoraInicio)
            .ToListAsync(cancellationToken);
    }
}