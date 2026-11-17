using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;

namespace ArenaBook.Core.Services;

public class ReservaService : IReservaService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IQuadraRepository _quadraRepository;
    private readonly IReservaRepository _reservaRepository;

    private static readonly IReadOnlyCollection<StatusReserva>
        StatusBloqueantes = Array.AsReadOnly(new[]
        {
            StatusReserva.Pendente,
            StatusReserva.Confirmada
        });

    public ReservaService(
        IClienteRepository clienteRepository,
        IQuadraRepository quadraRepository,
        IReservaRepository reservaRepository)
    {
        _clienteRepository = clienteRepository;
        _quadraRepository = quadraRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<Reserva> RegistrarAsync(
        int idCliente,
        int idQuadra,
        int idEsporte,
        DateOnly dataReserva,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        CancellationToken cancellationToken = default)
    {
        if (idCliente <= 0)
        {
            throw new ArgumentException(
                "O cliente deve ter identificador positivo.");
        }

        if (!await _clienteRepository.ExisteAsync(
                idCliente, cancellationToken))
        {
            throw new InvalidOperationException("Cliente não cadastrado.");
        }

        var quadra = await ValidarQuadraEIntervaloAsync(
            idQuadra,
            idEsporte,
            dataReserva,
            horaInicio,
            horaFim,
            cancellationToken);

        var duracaoHoras =
            (horaFim.Ticks - horaInicio.Ticks) / TimeSpan.TicksPerHour;

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

        var registrada =
            await _reservaRepository.TentarRegistrarSemSobreposicaoAsync(
                reserva, StatusBloqueantes, cancellationToken);

        if (!registrada)
        {
            throw new InvalidOperationException(
                "A quadra já possui uma reserva nesse intervalo.");
        }

        return reserva;
    }

    public async Task<bool> VerificarDisponibilidadeAsync(
        int idQuadra,
        int idEsporte,
        DateOnly dataReserva,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        CancellationToken cancellationToken = default)
    {
        await ValidarQuadraEIntervaloAsync(
            idQuadra,
            idEsporte,
            dataReserva,
            horaInicio,
            horaFim,
            cancellationToken);

        var existeSobreposicao =
            await _reservaRepository.ExisteSobreposicaoAsync(
                idQuadra,
                dataReserva,
                horaInicio,
                horaFim,
                StatusBloqueantes,
                cancellationToken);

        return !existeSobreposicao;
    }

    private async Task<Quadra> ValidarQuadraEIntervaloAsync(
        int idQuadra,
        int idEsporte,
        DateOnly dataReserva,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        CancellationToken cancellationToken)
    {
        if (idQuadra <= 0 || idEsporte <= 0)
        {
            throw new ArgumentException(
                "Quadra e modalidade devem ter identificadores positivos.");
        }

        if (horaInicio.Ticks % TimeSpan.TicksPerHour != 0 ||
            horaFim.Ticks % TimeSpan.TicksPerHour != 0)
        {
            throw new ArgumentException(
                "Os horários devem estar em horas inteiras.");
        }

        if (horaFim <= horaInicio)
        {
            throw new ArgumentException(
                "O horário final deve ser posterior ao inicial, no mesmo dia.");
        }

        var quadra = await _quadraRepository.ObterParaReservaAsync(
            idQuadra, cancellationToken);

        if (quadra is null)
        {
            throw new InvalidOperationException("Quadra não encontrada.");
        }

        if (!quadra.Ativa)
        {
            throw new InvalidOperationException("A quadra está desativada.");
        }

        if (!quadra.QuadraEsportes.Any(
                modalidade => modalidade.IdEsporte == idEsporte))
        {
            throw new InvalidOperationException(
                "A modalidade não é compatível com a quadra.");
        }

        var diaSemana = (byte)((int)dataReserva.DayOfWeek + 1);

        var dentroDoFuncionamento = quadra.HorariosFuncionamento.Any(
            horario => horario.DiaSemana == diaSemana &&
                horaInicio >= horario.HoraAbertura &&
                horaFim <= horario.HoraFechamento);

        if (!dentroDoFuncionamento)
        {
            throw new InvalidOperationException(
                "O intervalo está fora do horário de funcionamento.");
        }

        return quadra;
    }
}