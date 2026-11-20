using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Models;

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

    public ReservaService(IClienteRepository clienteRepository, IQuadraRepository quadraRepository, IReservaRepository reservaRepository)
    {
        _clienteRepository = clienteRepository;
        _quadraRepository = quadraRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<Reserva> RegistrarAsync(int idCliente, int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default)
    {
        if (idCliente <= 0)
        {
            throw new ArgumentException("O cliente deve ter identificador positivo.");
        }

        var cliente = await _clienteRepository.ObterPorIdAsync(idCliente, cancellationToken);

        if (cliente is null)
        {
            throw new InvalidOperationException("Cliente não cadastrado.");
        }

        var quadra = await ValidarQuadraEIntervaloAsync(idQuadra, idEsporte, dataReserva, horaInicio, horaFim, cancellationToken);

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

        var registrada = await _reservaRepository.TentarRegistrarSemSobreposicaoAsync(reserva, StatusBloqueantes, cancellationToken);

        if (!registrada)
        {
            throw new InvalidOperationException("A quadra já possui uma reserva nesse intervalo.");
        }
        return reserva;
    }

    public async Task<bool> VerificarDisponibilidadeAsync(int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken = default)
    {
        await ValidarQuadraEIntervaloAsync(idQuadra, idEsporte, dataReserva, horaInicio, horaFim, cancellationToken);

        var existeSobreposicao = await _reservaRepository.ExisteSobreposicaoAsync(idQuadra, dataReserva, horaInicio, horaFim, StatusBloqueantes, cancellationToken);

        return !existeSobreposicao;
    }

    private async Task<Quadra> ValidarQuadraEIntervaloAsync(int idQuadra, int idEsporte, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, CancellationToken cancellationToken)
    {
        if (idQuadra <= 0 || idEsporte <= 0)
        {
            throw new ArgumentException("Quadra e modalidade devem ter identificadores positivos.");
        }

        if (horaInicio.Ticks % TimeSpan.TicksPerHour != 0 || horaFim.Ticks % TimeSpan.TicksPerHour != 0)
        {
            throw new ArgumentException("Os horários devem estar em horas inteiras.");
        }

        if (horaFim <= horaInicio)
        {
            throw new ArgumentException("O horário final deve ser posterior ao inicial, no mesmo dia.");
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

        if (!quadra.QuadraEsportes.Any(
                modalidade => modalidade.IdEsporte == idEsporte))
        {
            throw new InvalidOperationException("A modalidade não é compatível com a quadra.");
        }

        var diaSemana = (byte)((int)dataReserva.DayOfWeek + 1);

        var dentroDoFuncionamento = quadra.HorariosFuncionamento.Any(h =>
            h.DiaSemana == diaSemana && horaInicio >= h.HoraAbertura && horaFim <= h.HoraFechamento);

        if (!dentroDoFuncionamento)
        {
            throw new InvalidOperationException("O intervalo está fora do horário de funcionamento.");
        }

        return quadra;
    }

    public async Task<Reserva> CancelarAsync(int idReserva, CancellationToken cancellationToken = default)
    {
        if (idReserva <= 0)
        {
            throw new ArgumentException("A reserva deve ter identificador positivo.");
        }

        var reserva = await _reservaRepository.ObterPorIdAsync(idReserva, cancellationToken);

        if (reserva is null)
        {
            throw new InvalidOperationException("Reserva não encontrada.");
        }

        if (!StatusBloqueantes.Contains(reserva.IdStatus))
        {
            throw new InvalidOperationException("Somente reservas ativas podem ser canceladas.");
        }

        var cancelada = await _reservaRepository.TentarCancelarAsync(idReserva, StatusBloqueantes, cancellationToken);

        if (!cancelada)
        {
            throw new InvalidOperationException("A reserva não está mais ativa para cancelamento.");
        }

        reserva.IdStatus = StatusReserva.Cancelada;

        return reserva;
    }
    public async Task<IReadOnlyList<HorarioDisponivel>> SugerirHorariosAsync(int idQuadra, DateOnly dataReserva, int duracaoHoras, CancellationToken cancellationToken = default)
    {
        if (idQuadra <= 0)
        {
            throw new ArgumentException("A quadra deve ter identificador positivo.");
        }

        if (duracaoHoras <= 0)
        {
            throw new ArgumentException("A duração deve ser um número positivo de horas.");
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

        var sugestoes = new List<HorarioDisponivel>();

        // Uma reserva precisa começar e terminar no mesmo dia.
        if (duracaoHoras >= 24)
        {
            return sugestoes;
        }

        var diaSemana = (int)dataReserva.DayOfWeek + 1;
        var funcionamento = quadra.HorariosFuncionamento.Where(h => h.DiaSemana == diaSemana).ToArray();

        for (var hora = 0; hora + duracaoHoras < 24; hora++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inicio = new TimeOnly(hora, 0);
            var fim = new TimeOnly(hora + duracaoHoras, 0);

            var dentroDoFuncionamento = funcionamento.Any(h => inicio >= h.HoraAbertura && fim <= h.HoraFechamento);

            if (!dentroDoFuncionamento)
            {
                continue;
            }

            var ocupado = await _reservaRepository.ExisteSobreposicaoAsync(idQuadra, dataReserva, inicio, fim, StatusBloqueantes, cancellationToken);

            if (!ocupado)
            {
                sugestoes.Add(new HorarioDisponivel(inicio, fim));
            }
        }

        return sugestoes;
    }
}