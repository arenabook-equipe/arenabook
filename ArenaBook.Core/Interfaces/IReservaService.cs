using ArenaBook.Core.Entities;

namespace ArenaBook.Core.Interfaces;

public interface IReservaService
{
    //responsável por validar as regras, calcular o preço e registrar.
    Task<Reserva> RegistrarAsync(int idCliente, int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default);

    //verificará se o intervalo pode ser reservado, sem gravar nada.
    Task<bool> VerificarDisponibilidadeAsync(int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default);

    //cancelar a reserva, alterando o status para cancelado.
    Task<Reserva> CancelarAsync(int idReserva, CancellationToken cancellationToken = default);
}