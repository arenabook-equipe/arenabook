using ArenaBook.Core.Entities;

namespace ArenaBook.Api.Contracts;

public record CriarReservaRequest(int IdQuadra, int IdEsporte, int IdCliente,
    DateOnly DataReserva, TimeOnly HoraInicio, TimeOnly HoraFim);

public record ReservaResponse(int IdReserva, int IdQuadra, int IdEsporte, int IdCliente,
    DateOnly DataReserva, TimeOnly HoraInicio, TimeOnly HoraFim, string Status, decimal ValorTotal)
{
    public static ReservaResponse De(Reserva r) => new(
        r.IdReserva, r.IdQuadra, r.IdEsporte, r.IdCliente,
        r.DataReserva, r.HoraInicio, r.HoraFim, r.IdStatus.ToString(), r.ValorTotal);
}
