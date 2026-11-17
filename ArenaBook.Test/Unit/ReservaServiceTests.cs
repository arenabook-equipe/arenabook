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

    [Test]
    //deve ser recusada a tentativa de reserva sobreposta a outra já confirmada
    public void R02_ReservaSobreposta()
    {
        var dataReserva = new DateOnly(2030, 1, 6);

        const int futsal = 1;
        const int volei = 2;

        var quadra = new Quadra
        {
            IdQuadra = 1,
            Nome = "Quadra poliesportiva",
            Ativa = true,
            PrecoHora = 80m
        };

        // As duas modalidades são permitidas nesta quadra
        quadra.QuadraEsportes.Add(new QuadraEsporte
        {
            IdQuadra = 1,
            IdEsporte = futsal
        });

        quadra.QuadraEsportes.Add(new QuadraEsporte
        {
            IdQuadra = 1,
            IdEsporte = volei
        });

        // Os dois intervalos estão dentro do funcionamento
        quadra.HorariosFuncionamento.Add(new HorarioFuncionamento
        {
            IdHorario = 1,
            IdQuadra = 1,
            DiaSemana = (byte)((int)dataReserva.DayOfWeek + 1),
            HoraAbertura = new TimeOnly(8, 0),
            HoraFechamento = new TimeOnly(22, 0)
        });

        var reservaOriginal = new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = futsal,
            DataReserva = dataReserva,
            HoraInicio = new TimeOnly(18, 0),
            HoraFim = new TimeOnly(20, 0),
            ValorTotal = 160m,
            IdStatus = StatusReserva.Confirmada
        };

        var reservaRepository = new ReservaRepositoryFake();
        reservaRepository.ReservasSalvas.Add(reservaOriginal);

        var service = new ReservaService(
            new ClienteRepositoryFake(),
            new QuadraRepositoryFake(quadra),
            reservaRepository);

        // A tentativa deve ser recusada por sobreposição.
        var excecao = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.RegistrarAsync(
                idCliente: 1,
                idQuadra: 1,
                idEsporte: volei,
                dataReserva: dataReserva,
                horaInicio: new TimeOnly(19, 0),
                horaFim: new TimeOnly(21, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(excecao!.Message,
                Is.EqualTo("A quadra já possui uma reserva nesse intervalo."));

            // Nenhuma nova reserva foi salva.
            Assert.That(reservaRepository.ReservasSalvas, Has.Count.EqualTo(1));
            Assert.That(reservaRepository.ReservasSalvas[0],
                Is.SameAs(reservaOriginal));

            // A reserva original permanece confirmada e com os mesmos dados.
            Assert.That(reservaOriginal.IdStatus,
                Is.EqualTo(StatusReserva.Confirmada));
            Assert.That(reservaOriginal.IdEsporte, Is.EqualTo(futsal));
            Assert.That(reservaOriginal.HoraInicio, Is.EqualTo(new TimeOnly(18, 0)));
            Assert.That(reservaOriginal.HoraFim, Is.EqualTo(new TimeOnly(20, 0)));
            Assert.That(reservaOriginal.ValorTotal, Is.EqualTo(160m));
        });
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
            var existeSobreposicao = ReservasSalvas.Any(reserva =>
                reserva.IdQuadra == idQuadra &&
                reserva.DataReserva == dataReserva &&
                statusBloqueantes.Contains(reserva.IdStatus) &&
                reserva.HoraInicio < horaFim &&
                reserva.HoraFim > horaInicio);

            return Task.FromResult(existeSobreposicao);
        }

        public async Task<bool> TentarRegistrarSemSobreposicaoAsync(Reserva reserva, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default)
        {
            var existeSobreposicao = await ExisteSobreposicaoAsync(reserva.IdQuadra, reserva.DataReserva, reserva.HoraInicio, reserva.HoraFim, statusBloqueantes, cancellationToken);

            if (existeSobreposicao)
            {
                return false;
            }

            reserva.IdReserva = ReservasSalvas.Count == 0 ? 1 : ReservasSalvas.Max(r => r.IdReserva) + 1;

            ReservasSalvas.Add(reserva);

            return true;
        }
    }
}