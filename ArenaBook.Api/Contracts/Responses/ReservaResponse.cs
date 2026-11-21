using ArenaBook.Core.Entities;

namespace ArenaBook.Api.Contracts.Responses;

public record ReservaResponse(int IdReserva, int IdQuadra, int IdEsporte, int IdCliente, DateOnly DataReserva, TimeOnly HoraInicio, TimeOnly HoraFim, string Status, decimal ValorTotal)
{
    public static ReservaResponse CriarResposta(Reserva reserva) => new(reserva.IdReserva, reserva.IdQuadra, reserva.IdEsporte, reserva.IdCliente, reserva.DataReserva, reserva.HoraInicio, reserva.HoraFim, reserva.IdStatus.ToString(), reserva.ValorTotal);
}
