using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;

namespace ArenaBook.Test.Unit;

[Category("Unit")]
public class ReservaServiceTests
{
    [Test]
    public async Task R01_ReservaDuasHoras()
    {
        //cliente cadastrado e quadra livre a 80 por hora
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();
        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)), repository);

        //reservar das 18h as 20h
        var reserva = await service.RegistrarAsync(1, 1, 1, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        //duas horas custam 160
        Assert.That(reserva.IdReserva, Is.GreaterThan(0));
        Assert.That(reserva.ValorTotal, Is.EqualTo(160m));
        Assert.That(reserva.IdStatus, Is.EqualTo(StatusReserva.Pendente));
        Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
        Assert.That(repository.ReservasSalvas[0], Is.SameAs(reserva));
    }

    [Test]
    public async Task R02_ReservaSobreposta()
    {
        //futsal e volei permitidos na mesma quadra
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();
        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)), repository);
        var original = await service.RegistrarAsync(1, 1, 1, data, new TimeOnly(18, 0), new TimeOnly(20, 0));
        original.IdStatus = StatusReserva.Confirmada;

        //tentar reservar volei das 19h as 21h na quadra ocupada
        var erro = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.RegistrarAsync(1, 1, 2, data, new TimeOnly(19, 0), new TimeOnly(21, 0));
        });

        //a tentativa foi recusada e a original permanece intacta
        Assert.That(erro!.Message, Is.EqualTo("A quadra já possui uma reserva nesse intervalo."));
        Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
        Assert.That(repository.ReservasSalvas[0], Is.SameAs(original));
        Assert.That(original.IdStatus, Is.EqualTo(StatusReserva.Confirmada));
        Assert.That(original.IdEsporte, Is.EqualTo(1));
        Assert.That(original.HoraInicio, Is.EqualTo(new TimeOnly(18, 0)));
        Assert.That(original.HoraFim, Is.EqualTo(new TimeOnly(20, 0)));
        Assert.That(original.ValorTotal, Is.EqualTo(160m));
    }

    [Test]
    public async Task R03_CancelarReserva()
    {
        // reserva ativa das 18h as 20h
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();
        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)), repository);
        var original = await service.RegistrarAsync(1, 1, 1, data, new TimeOnly(18, 0), new TimeOnly(20, 0));
        var antes = await service.VerificarDisponibilidadeAsync(1, 2, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        // cancelar e verificar a disponibilidade novamente
        var cancelada = await service.CancelarAsync(original.IdReserva);
        var depois = await service.VerificarDisponibilidadeAsync(1, 2, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        //libera o horario e preserva a reserva no historico
        Assert.That(antes, Is.False);
        Assert.That(depois, Is.True);
        Assert.That(cancelada.IdReserva, Is.EqualTo(original.IdReserva));
        Assert.That(cancelada.IdStatus, Is.EqualTo(StatusReserva.Cancelada));
        Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
        Assert.That(repository.ReservasSalvas[0], Is.SameAs(original));
        Assert.That(original.ValorTotal, Is.EqualTo(160m));
    }

    [Test]
    public async Task R04_SugerirHorarios()
    {
        //funcionamento das 18h as 22h, com reserva das 19h as 21h
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaDisponibilidade(data);
        quadra.HorariosFuncionamento.Single().HoraAbertura = new TimeOnly(18, 0);
        var repository = new ReservaRepositoryFake();
        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(quadra), repository);
        await service.RegistrarAsync(1, 1, 1, data, new TimeOnly(19, 0), new TimeOnly(21, 0));

        //pedir sugestoes para uma reserva de uma hora
        var sugestoes = await service.SugerirHorariosAsync(1, data, 1);
        var esperado = new[]
        {
            (new TimeOnly(18, 0), new TimeOnly(19, 0)),
            (new TimeOnly(21, 0), new TimeOnly(22, 0))
        };

        //retorna os horarios livres, inclusive os consecutivos
        Assert.That(sugestoes.Select(h => (h.HoraInicio, h.HoraFim)), Is.EqualTo(esperado));
        Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
    }


    private static Quadra CriarQuadraParaDisponibilidade(DateOnly data)
    {
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

        quadra.QuadraEsportes.Add(new QuadraEsporte
        {
            IdQuadra = 1,
            IdEsporte = 2
        });

        quadra.HorariosFuncionamento.Add(new HorarioFuncionamento
        {
            IdHorario = 1,
            IdQuadra = 1,
            DiaSemana = (byte)((int)data.DayOfWeek + 1),
            HoraAbertura = new TimeOnly(8, 0),
            HoraFechamento = new TimeOnly(22, 0)
        });

        return quadra;
    }
    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        public Task<Cliente?> ObterPorIdAsync(int idCliente, CancellationToken cancellationToken)
        {
            Cliente? cliente = idCliente == 1 ? new Cliente
            {
                IdCliente = 1 
            } : null;

            return Task.FromResult(cliente);
        }

        public Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Consulta por e-mail não utilizada nos testes de reservas.");
        }

        public Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Cadastro de cliente não utilizado nos testes de reservas.");
        }

        public Task<bool> AtualizarAsync(int idCliente, string nome, string telefone, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Atualização de cliente não utilizada nos testes de reservas.");
        }

        public Task<ResultadoExclusaoCliente> ExcluirAsync(int idCliente, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Exclusão de cliente não utilizada nos testes de reservas.");
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
        public Task<Reserva?> ObterPorIdAsync(int idReserva, CancellationToken cancellationToken = default)
        {
            var reserva = ReservasSalvas.SingleOrDefault(item => item.IdReserva == idReserva);

            return Task.FromResult(reserva);
        }

        public Task<bool> TentarCancelarAsync(int idReserva, IReadOnlyCollection<StatusReserva> statusPermitidos, CancellationToken cancellationToken = default)
        {
            var reserva = ReservasSalvas.SingleOrDefault(item => item.IdReserva == idReserva);

            if (reserva is null || !statusPermitidos.Contains(reserva.IdStatus))
            {
                return Task.FromResult(false);
            }

            reserva.IdStatus = StatusReserva.Cancelada;

            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Reserva>> ConsultarAgendaAsync(int idQuadra, DateOnly dataReserva, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Reserva> agenda = ReservasSalvas
                .Where(reserva => reserva.IdQuadra == idQuadra && reserva.DataReserva == dataReserva && (reserva.IdStatus == StatusReserva.Pendente || reserva.IdStatus == StatusReserva.Confirmada))
                .OrderBy(reserva => reserva.HoraInicio)
                .ThenBy(reserva => reserva.IdReserva)
                .ToList();

            return Task.FromResult(agenda);
        }
    }
}