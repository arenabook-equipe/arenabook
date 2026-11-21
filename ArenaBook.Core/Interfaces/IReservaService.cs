using ArenaBook.Core.Entities;
using ArenaBook.Core.Models;

namespace ArenaBook.Core.Interfaces;

public interface IReservaService
{
    //responsável por validar as regras, calcular o preço e registrar
    Task<Reserva> RegistrarAsync(int idCliente, int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default);

    //verifica se o intervalo pode ser reservado
    Task<bool> VerificarDisponibilidadeAsync(int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default);

    //cancelar a reserva, alterando o status para cancelado
    Task<Reserva> CancelarAsync(int idReserva, CancellationToken cancellationToken = default);

    // Retorna sugestões de horários livres com a duração solicitada
    Task<IReadOnlyList<HorarioDisponivel>> SugerirHorariosAsync(int idQuadra, DateOnly dataReserva, int duracaoHoras, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reserva>> ConsultarAgendaAsync(int idQuadra, DateOnly dataReserva, CancellationToken cancellationToken = default);

}