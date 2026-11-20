using ArenaBook.Core.Entities;

namespace ArenaBook.Core.Interfaces;

public interface IReservaService
{
    Task<Reserva> CriarAsync(int idQuadra, int idEsporte, int idCliente,
        DateOnly data, TimeOnly inicio, TimeOnly fim);
    Task<List<Reserva>> ObterAgendaAsync(int idQuadra, DateOnly data);
    Task CancelarAsync(int idReserva);
}
