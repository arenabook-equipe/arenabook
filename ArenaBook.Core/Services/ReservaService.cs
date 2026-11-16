using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;

namespace ArenaBook.Core.Services;

public class ReservaService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IQuadraRepository _quadraRepository;
    private readonly IReservaRepository _reservaRepository;

    //status que bloqueiam o intervalo, impedindo sobreposição de reservas.  
    private static readonly IReadOnlyCollection<StatusReserva> StatusBloqueantes = Array.AsReadOnly(new[]
        {
            StatusReserva.Pendente,
            StatusReserva.Confirmada
        });

    public ReservaService(IClienteRepository clienteRepository, IQuadraRepository quadraRepository, IReservaRepository reservaRepository)
    {
        _clienteRepository = clienteRepository;
        _quadraRepository = quadraRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<Reserva> RegistrarAsync(int idCliente,int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default)
    {
        if (idCliente <= 0 || idQuadra <= 0 || idEsporte <= 0)
        {
            throw new ArgumentException("Cliente, quadra e modalidade devem ter identificadores positivos.");
        }

        //verifica se os horários estão em horas inteiras e se o horário final é posterior ao inicial.
        if (horaInicio.Ticks % TimeSpan.TicksPerHour != 0 || horaFim.Ticks % TimeSpan.TicksPerHour != 0)
        {
            throw new ArgumentException("Os horários devem estar em horas inteiras.");
        }
        if (horaFim <= horaInicio)
        {
            throw new ArgumentException("O horário final deve ser posterior ao inicial, no mesmo dia.");
        }

        var clienteExiste = await _clienteRepository.ExisteAsync(idCliente, cancellationToken);

        if (!clienteExiste)
        {
            throw new InvalidOperationException("Cliente não cadastrado.");
        }

        var quadra = await _quadraRepository.ObterParaReservaAsync(idQuadra, cancellationToken);

        if (quadra is null)
        {
            throw new InvalidOperationException("Quadra não encontrada.");
        }
        if (!quadra.Ativa)
        {
            throw new InvalidOperationException("A quadra está desativada.");
        }

        var modalidadeCompativel = quadra.QuadraEsportes.Any(modalidade => modalidade.IdEsporte == idEsporte);

        if (!modalidadeCompativel)
        {
            throw new InvalidOperationException("A modalidade não é compatível com a quadra.");
        }

        //converte o dia para domingo = 1
        var diaSemana = (byte)((int)dataReserva.DayOfWeek + 1);

        var dentroDoFuncionamento = quadra.HorariosFuncionamento.Any(
            horario => horario.DiaSemana == diaSemana &&
                horaInicio >= horario.HoraAbertura &&
                horaFim <= horario.HoraFechamento);

        if (!dentroDoFuncionamento)
        {
            throw new InvalidOperationException("O intervalo está fora do horário de funcionamento.");
        }

        //calcula as horas
        var duracaoHoras = (horaFim.Ticks - horaInicio.Ticks) / TimeSpan.TicksPerHour;

        var reserva = new Reserva
        {
            IdCliente = idCliente,
            IdQuadra = idQuadra,
            IdEsporte = idEsporte,
            DataReserva = dataReserva,
            HoraInicio = horaInicio,
            HoraFim = horaFim,
            ValorTotal = duracaoHoras * quadra.PrecoHora,
            IdStatus = StatusReserva.Pendente
        };

        // verifica conflito e grava
        var registrada = await _reservaRepository.TentarRegistrarSemSobreposicaoAsync(reserva, StatusBloqueantes, cancellationToken);

        if (!registrada)
        {
            throw new InvalidOperationException("A quadra já possui uma reserva nesse intervalo.");
        }

        return reserva;
    }
}