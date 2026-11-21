using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;

namespace ArenaBook.Core.Interfaces;

public interface IReservaRepository
{
    //consultará se existe reserva bloqueando o intervalo.
    Task<bool> ExisteSobreposicaoAsync(int idQuadra, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default);

    //verificará novamente o conflito e salvará com proteção contra solicitações simultâneas.
    Task<bool> TentarRegistrarSemSobreposicaoAsync(Reserva reserva, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default);

    //consultará a reserva pelo id.
    Task<Reserva?> ObterPorIdAsync(int idReserva, CancellationToken cancellationToken = default);

    //consultará a reserva pelo id e status.
    Task<bool> TentarCancelarAsync(int idReserva, IReadOnlyCollection<StatusReserva> statusPermitidos, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reserva>> ConsultarAgendaAsync(int idQuadra, DateOnly dataReserva, CancellationToken cancellationToken = default);
}