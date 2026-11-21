using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;
using NUnit.Framework;
using ArenaBook.Core.Exceptions;

namespace ArenaBook.Test.Unit;

public class ReservaServiceTests
{
    [Test]
    //deve ser possível registrar uma reserva de duas horas
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
        var excecao = Assert.ThrowsAsync<ConflitoReservaException>(
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

    [TestCase(16, 18, true)]  // Antes da reserva existente.
    [TestCase(19, 21, false)] // Sobreposição entre modalidades.
    [TestCase(18, 20, false)] // Mesmo intervalo.
    [TestCase(20, 21, true)]  // Começa quando a anterior termina.
    public async Task Disponibilidade_DeveConsiderarOcupacaoDaQuadra(int horaInicio, int horaFim, bool esperado)
    {
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaDisponibilidade(data);
        var repository = new ReservaRepositoryFake();

        repository.ReservasSalvas.Add(new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = 1,
            DataReserva = data,
            HoraInicio = new TimeOnly(18, 0),
            HoraFim = new TimeOnly(20, 0),
            ValorTotal = 160m,
            IdStatus = StatusReserva.Confirmada
        });

        IReservaService service = new ReservaService(
            new ClienteRepositoryFake(),
            new QuadraRepositoryFake(quadra),
            repository);

        var disponivel = await service.VerificarDisponibilidadeAsync(
            idQuadra: 1,
            idEsporte: 2,
            dataReserva: data,
            horaInicio: new TimeOnly(horaInicio, 0),
            horaFim: new TimeOnly(horaFim, 0));

        Assert.Multiple(() =>
        {
            Assert.That(disponivel, Is.EqualTo(esperado));
            Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Disponibilidade_QuadraLivre_NaoDeveCriarReserva()
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        IReservaService service = new ReservaService(
            new ClienteRepositoryFake(),
            new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)),
            repository);

        var disponivel = await service.VerificarDisponibilidadeAsync(
            1, 2, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        Assert.Multiple(() =>
        {
            Assert.That(disponivel, Is.True);
            Assert.That(repository.ReservasSalvas, Is.Empty);
        });
    }

    [TestCase(0, 2, true, 18, 0, 20, typeof(ArgumentException))]
    [TestCase(1, 0, true, 18, 0, 20, typeof(ArgumentException))]
    [TestCase(99, 2, true, 18, 0, 20, typeof(KeyNotFoundException))]
    [TestCase(1, 2, false, 18, 0, 20, typeof(ConflitoReservaException))]
    [TestCase(1, 99, true, 18, 0, 20, typeof(ArgumentException))]
    [TestCase(1, 2, true, 18, 30, 20, typeof(ArgumentException))]
    [TestCase(1, 2, true, 20, 0, 18, typeof(ArgumentException))]
    [TestCase(1, 2, true, 18, 0, 18, typeof(ArgumentException))]
    [TestCase(1, 2, true, 7, 0, 9, typeof(ArgumentException))]
    [TestCase(1, 2, true, 21, 0, 23, typeof(ArgumentException))]
    public void Disponibilidade_DadosInvalidos_DeveRecusar(
        int idQuadra,
        int idEsporte,
        bool ativa,
        int horaInicio,
        int minutoInicio,
        int horaFim,
        Type tipoExcecao)
    {
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaDisponibilidade(data);
        quadra.Ativa = ativa;

        var repository = new ReservaRepositoryFake();

        IReservaService service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(quadra), repository);

        Assert.ThrowsAsync(tipoExcecao, async () =>
        {
            await service.VerificarDisponibilidadeAsync(
                idQuadra,
                idEsporte,
                data,
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFim, 0));
        });

        Assert.That(repository.ReservasSalvas, Is.Empty);
    }

    // Cliente inexistente.
    [TestCase(99, 1, 2, true, 18, 0, 20, 0,
        typeof(KeyNotFoundException))]

    // Quadra inexistente.
    [TestCase(1, 99, 2, true, 18, 0, 20, 0,
        typeof(KeyNotFoundException))]

    // Quadra desativada.
    [TestCase(1, 1, 2, false, 18, 0, 20, 0,
        typeof(ConflitoReservaException))]

    // Modalidade incompatível.
    [TestCase(1, 1, 99, true, 18, 0, 20, 0,
        typeof(ArgumentException))]

    // Início com minutos.
    [TestCase(1, 1, 2, true, 18, 30, 20, 0,
        typeof(ArgumentException))]

    // Fim com minutos.
    [TestCase(1, 1, 2, true, 18, 0, 20, 30,
        typeof(ArgumentException))]

    // Fim anterior ao início.
    [TestCase(1, 1, 2, true, 20, 0, 18, 0,
        typeof(ArgumentException))]

    // Início igual ao fim.
    [TestCase(1, 1, 2, true, 18, 0, 18, 0,
        typeof(ArgumentException))]

    // Tentativa de atravessar a meia-noite.
    [TestCase(1, 1, 2, true, 23, 0, 1, 0,
        typeof(ArgumentException))]

    // Antes da abertura.
    [TestCase(1, 1, 2, true, 7, 0, 9, 0,
        typeof(ArgumentException))]

    // Depois do fechamento.
    [TestCase(1, 1, 2, true, 21, 0, 23, 0,
        typeof(ArgumentException))]

    // Intervalo fora do funcionamento(deve ser rejeitado)
    public void Criacao_DadosInvalidos(int idCliente, int idQuadra, int idEsporte, bool ativa, int horaInicio, int minutoInicio, int horaFim, int minutoFim, Type tipoExcecao)
    {
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaDisponibilidade(data);
        quadra.Ativa = ativa;

        var repository = new ReservaRepositoryFake();

        var service = new ReservaService(
            new ClienteRepositoryFake(),
            new QuadraRepositoryFake(quadra),
            repository);

        Assert.ThrowsAsync(tipoExcecao, async () =>
        {
            await service.RegistrarAsync(
                idCliente,
                idQuadra,
                idEsporte,
                data,
                new TimeOnly(horaInicio, minutoInicio),
                new TimeOnly(horaFim, minutoFim));
        });

        Assert.That(repository.ReservasSalvas, Is.Empty);
    }

    [TestCase(18, 20, false)] // Intervalo duplicado
    [TestCase(16, 18, true)]  // Termina quando a existente começa
    [TestCase(20, 21, true)]  // Começa quando a existente termina
    // A quadra está ocupada das 18h às 20h, mas a reserva é de outra modalidade
    public async Task Criacao_DeveRespeitarOcupacaoEPermitirConsecutivos(int horaInicio, int horaFim, bool deveRegistrar)
    {
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaDisponibilidade(data);
        var repository = new ReservaRepositoryFake();

        var original = new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = 1,
            DataReserva = data,
            HoraInicio = new TimeOnly(18, 0),
            HoraFim = new TimeOnly(20, 0),
            ValorTotal = 160m,
            IdStatus = StatusReserva.Confirmada
        };

        repository.ReservasSalvas.Add(original);

        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(quadra), repository);

        if (deveRegistrar)
        {
            var resultado = await service.RegistrarAsync(1, 1, 2, data, new TimeOnly(horaInicio, 0), new TimeOnly(horaFim, 0));

            Assert.Multiple(() =>
            {
                Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(2));
                Assert.That(repository.ReservasSalvas[1], Is.SameAs(resultado));
                Assert.That(resultado.IdStatus, Is.EqualTo(StatusReserva.Pendente));
                Assert.That(resultado.HoraInicio,
                    Is.EqualTo(new TimeOnly(horaInicio, 0)));
                Assert.That(resultado.HoraFim,
                    Is.EqualTo(new TimeOnly(horaFim, 0)));
            });
        }
        else
        {
            Assert.ThrowsAsync<ConflitoReservaException>(async () =>
            {
                await service.RegistrarAsync(
                    1, 1, 2, data,
                    new TimeOnly(horaInicio, 0),
                    new TimeOnly(horaFim, 0));
            });

            Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
        }

        Assert.Multiple(() =>
        {
            Assert.That(repository.ReservasSalvas[0], Is.SameAs(original));
            Assert.That(original.IdStatus, Is.EqualTo(StatusReserva.Confirmada));
            Assert.That(original.HoraInicio, Is.EqualTo(new TimeOnly(18, 0)));
            Assert.That(original.HoraFim, Is.EqualTo(new TimeOnly(20, 0)));
        });
    }

    [TestCase(StatusReserva.Pendente)]
    [TestCase(StatusReserva.Confirmada)]
    // A reserva cancelada deve liberar o horário para nova reserva e manter o histórico da reserva original
    public async Task R03_CancelarReserva(StatusReserva statusInicial)
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        IReservaService service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)), repository);

        var original = await service.RegistrarAsync(1, 1, 1, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        original.IdStatus = statusInicial;

        var disponivelAntes = await service.VerificarDisponibilidadeAsync(1, 2, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        Assert.That(disponivelAntes, Is.False);

        var cancelada = await service.CancelarAsync(original.IdReserva);

        var disponivelDepois = await service.VerificarDisponibilidadeAsync(1, 2, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        Assert.That(disponivelDepois, Is.True);

        var nova = await service.RegistrarAsync(1, 1, 2, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        Assert.Multiple(() =>
        {
            Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(2));
            Assert.That(repository.ReservasSalvas[0], Is.SameAs(original));

            Assert.That(cancelada.IdReserva, Is.EqualTo(original.IdReserva));
            Assert.That(original.IdStatus, Is.EqualTo(StatusReserva.Cancelada));
            Assert.That(original.HoraInicio, Is.EqualTo(new TimeOnly(18, 0)));
            Assert.That(original.HoraFim, Is.EqualTo(new TimeOnly(20, 0)));
            Assert.That(original.ValorTotal, Is.EqualTo(160m));

            Assert.That(nova.IdReserva, Is.Not.EqualTo(original.IdReserva));
            Assert.That(nova.IdStatus, Is.EqualTo(StatusReserva.Pendente));
            Assert.That(nova.ValorTotal, Is.EqualTo(160m));
        });
    }

    [TestCase(StatusReserva.Pendente)]
    [TestCase(StatusReserva.Confirmada)]
    // deve sugerir horários livres, considerando reservas existentes e duração solicitada
    public async Task R04_SugerirHorarios(StatusReserva status)
    {
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaSugestoes(data);
        var repository = new ReservaRepositoryFake();

        repository.ReservasSalvas.Add(new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = 1,
            DataReserva = data,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(21, 0),
            ValorTotal = 160m,
            IdStatus = status
        });

        IReservaService service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(quadra), repository);

        var sugestoes = await service.SugerirHorariosAsync(1, data, 1);

        var esperado = new[]
        {
        (new TimeOnly(18, 0), new TimeOnly(19, 0)),
        (new TimeOnly(21, 0), new TimeOnly(22, 0))
    };

        Assert.Multiple(() =>
        {
            Assert.That(sugestoes.Select(h => (h.HoraInicio, h.HoraFim)), Is.EqualTo(esperado));
            Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
            Assert.That(repository.ReservasSalvas[0].IdStatus, Is.EqualTo(status));
        });
    }

    [TestCase(2)]
    [TestCase(5)]
    // deve retornar lista vazia quando não houver espaço para a duração
    public async Task Sugestoes_SemEspacoParaDuracao(int duracaoHoras)
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        repository.ReservasSalvas.Add(new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = 1,
            DataReserva = data,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(21, 0),
            ValorTotal = 160m,
            IdStatus = StatusReserva.Confirmada
        });

        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaSugestoes(data)), repository);

        var sugestoes = await service.SugerirHorariosAsync(1, data, duracaoHoras);

        Assert.That(sugestoes, Is.Empty);
    }

    [TestCase(StatusReserva.Cancelada)]
    [TestCase(StatusReserva.Concluida)]
    //deve sugerir horários livres mesmo com reserva existente, se a reserva não estiver ativa
    public async Task Sugestoes_ReservaNaoAtiva(StatusReserva status)
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        repository.ReservasSalvas.Add(new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = 1,
            DataReserva = data,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(21, 0),
            ValorTotal = 160m,
            IdStatus = status
        });

        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaSugestoes(data)), repository);

        var sugestoes = await service.SugerirHorariosAsync(1, data, 2);

        var esperado = new[]
        {
        (new TimeOnly(18, 0), new TimeOnly(20, 0)),
        (new TimeOnly(19, 0), new TimeOnly(21, 0)),
        (new TimeOnly(20, 0), new TimeOnly(22, 0))
    };

        Assert.That(sugestoes.Select(h => (h.HoraInicio, h.HoraFim)), Is.EqualTo(esperado));
    }

    [TestCase(0)]
    [TestCase(-1)]
    // deve recusar solicitações de sugestão com duração inválida
    public void Sugestoes_DuracaoInvalida(int duracaoHoras)
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();
        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaSugestoes(data)), repository);

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.SugerirHorariosAsync(1, data, duracaoHoras);
        });
    }

    [TestCase(99, true)]
    [TestCase(1, false)]
    // deve recusar solicitações de sugestão para quadra inexistente ou desativada
    public void Sugestoes_QuadraInexistenteOuDesativada(int idQuadra, bool ativa)
    {
        var data = new DateOnly(2030, 1, 6);
        var quadra = CriarQuadraParaSugestoes(data);
        quadra.Ativa = ativa;

        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(quadra), new ReservaRepositoryFake());

        Assert.ThrowsAsync(idQuadra == 99 ? typeof(KeyNotFoundException) : typeof(ConflitoReservaException), async () =>
        {
            await service.SugerirHorariosAsync(idQuadra, data, 1);
        });
    }

    private static Quadra CriarQuadraParaSugestoes(DateOnly data)
    {
        var quadra = CriarQuadraParaDisponibilidade(data);
        quadra.HorariosFuncionamento.Clear();

        quadra.HorariosFuncionamento.Add(new HorarioFuncionamento
        {
            IdHorario = 1,
            IdQuadra = 1,
            DiaSemana = (byte)((int)data.DayOfWeek + 1),
            HoraAbertura = new TimeOnly(18, 0),
            HoraFechamento = new TimeOnly(22, 0)
        });

        return quadra;
    }

    [Test]
    public async Task Cancelamento_SegundaTentativa_DeveSerRecusada()
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)), repository);

        var reserva = await service.RegistrarAsync(1, 1, 1, data, new TimeOnly(18, 0), new TimeOnly(20, 0));

        await service.CancelarAsync(reserva.IdReserva);

        Assert.ThrowsAsync<ConflitoReservaException>(async () =>
        {
            await service.CancelarAsync(reserva.IdReserva);
        });

        Assert.Multiple(() =>
        {
            Assert.That(repository.ReservasSalvas, Has.Count.EqualTo(1));
            Assert.That(reserva.IdStatus, Is.EqualTo(StatusReserva.Cancelada));
        });
    }

    [Test]
    public void Cancelamento_ReservaInexistente_DeveSerRecusado()
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        var service = new ReservaService(
            new ClienteRepositoryFake(),
            new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)),
            repository);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await service.CancelarAsync(99);
        });

        Assert.That(repository.ReservasSalvas, Is.Empty);
    }

    [Test]
    public void Cancelamento_ReservaConcluida()
    {
        var data = new DateOnly(2030, 1, 6);
        var repository = new ReservaRepositoryFake();

        var reserva = new Reserva
        {
            IdReserva = 1,
            IdCliente = 1,
            IdQuadra = 1,
            IdEsporte = 1,
            DataReserva = data,
            HoraInicio = new TimeOnly(18, 0),
            HoraFim = new TimeOnly(20, 0),
            ValorTotal = 160m,
            IdStatus = StatusReserva.Concluida
        };

        repository.ReservasSalvas.Add(reserva);

        var service = new ReservaService(new ClienteRepositoryFake(), new QuadraRepositoryFake(CriarQuadraParaDisponibilidade(data)), repository);

        Assert.ThrowsAsync<ConflitoReservaException>(async () =>
        {
            await service.CancelarAsync(1);
        });

        Assert.That(reserva.IdStatus, Is.EqualTo(StatusReserva.Concluida));
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
            Cliente? cliente = idCliente == 1
                ? new Cliente { IdCliente = 1, Nome = "Cliente de teste" }
                : null;
            return Task.FromResult(cliente);
        }

        public Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> AtualizarAsync(int idCliente, string nome, string telefone, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ResultadoExclusaoCliente> ExcluirAsync(int idCliente, CancellationToken cancellationToken)
            => throw new NotSupportedException();
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
                .Where(r => r.IdQuadra == idQuadra && r.DataReserva == dataReserva &&
                    (r.IdStatus == StatusReserva.Pendente || r.IdStatus == StatusReserva.Confirmada))
                .OrderBy(r => r.HoraInicio)
                .ToList();
            return Task.FromResult(agenda);
        }
    }
}