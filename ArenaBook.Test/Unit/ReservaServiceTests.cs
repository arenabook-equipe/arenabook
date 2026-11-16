using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;
using NUnit.Framework;

namespace ArenaBook.Test.Unit;

public class ReservaServiceTests
{
    [Test]
    public async Task R01_ReservaDuasHoras()
    {
        //cliente cadastrado e quadra disponível.
        var dataReserva = new DateOnly(2030, 1, 6);

        var quadra = new Quadra
        {
            IdQuadra = 1,
            Nome = "Quadra poliesportiva",
            Ativa = true,
            PrecoHora = 80m
        };

        quadra.QuadraEsportes.Add(new QuadraEsporte
        {
            IdQuadra = 1,
            IdEsporte = 1
        });

        quadra.HorariosFuncionamento.Add(new HorarioFuncionamento
        {
            IdHorario = 1,
            IdQuadra = 1,
            DiaSemana = (byte)((int)dataReserva.DayOfWeek + 1),
            HoraAbertura = new TimeOnly(8, 0),
            HoraFechamento = new TimeOnly(22, 0)
        });

        var clienteRepository = new ClienteRepositoryFake();
        var quadraRepository = new QuadraRepositoryFake(quadra);
        var reservaRepository = new ReservaRepositoryFake();

        var service = new ReservaService(clienteRepository, quadraRepository, reservaRepository);

        //solicitar reserva d duas horas.
        var resultado = await service.RegistrarAsync(
            idCliente: 1,
            idQuadra: 1,
            idEsporte: 1,
            dataReserva: dataReserva,
            horaInicio: new TimeOnly(18, 0),
            horaFim: new TimeOnly(20, 0));

        // Conferir: valor, status e solicitação de gravação.
        Assert.That(resultado.ValorTotal, Is.EqualTo(160m));
        Assert.That(resultado.IdStatus, Is.EqualTo(StatusReserva.Pendente));
        Assert.That(reservaRepository.ReservasSalvas, Has.Count.EqualTo(1));
        Assert.That(reservaRepository.ReservasSalvas[0], Is.SameAs(resultado));
    }

    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        public Task<bool> ExisteAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(idCliente == 1);
        }
    }

    private sealed class QuadraRepositoryFake : IQuadraRepository
    {
        private readonly Quadra _quadra;

        public QuadraRepositoryFake(Quadra quadra)
        {
            _quadra = quadra;
        }

        public Task<Quadra?> ObterParaReservaAsync(int idQuadra, CancellationToken cancellationToken = default)
        {
            Quadra? resultado = idQuadra == _quadra.IdQuadra ? _quadra : null;
            return Task.FromResult(resultado);
        }
    }

    private sealed class ReservaRepositoryFake : IReservaRepository
    {
        public List<Reserva> ReservasSalvas { get; } = new();

        public Task<bool> ExisteSobreposicaoAsync(int idQuadra, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default)
        {
            // Neste cenário R01, o horário está livre.
            return Task.FromResult(false);
        }

        public Task<bool> TentarRegistrarSemSobreposicaoAsync(Reserva reserva, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default)
        {
            reserva.IdReserva = ReservasSalvas.Count + 1;
            ReservasSalvas.Add(reserva);

            return Task.FromResult(true);
        }
    }
}