using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;

namespace ArenaBook.Test.Unit;

[Category("Unit")]
public class HorarioFuncionamentoServiceTests
{
    [Test]
    public async Task H01_CadastroValidoRetornaFuncionamento()
    {
        //quadra cadastrada e repositorio sem horarios
        var repository = new HorarioRepositoryFake();
        var service = new HorarioFuncionamentoService(new QuadraRepositoryFake(), repository);

        //configurar domingo das 8h as 22h e consultar
        var horario = await service.CadastrarAsync(1, 1, new TimeOnly(8, 0), new TimeOnly(22, 0));
        var horarios = await service.ConsultarAsync(1);

        //a consulta retorna a configuracao cadastrada
        Assert.That(horario.IdHorario, Is.GreaterThan(0));
        Assert.That(horarios, Has.Count.EqualTo(1));
        Assert.That(horarios[0].IdQuadra, Is.EqualTo(1));
        Assert.That(horarios[0].DiaSemana, Is.EqualTo(1));
        Assert.That(horarios[0].HoraAbertura, Is.EqualTo(new TimeOnly(8, 0)));
        Assert.That(horarios[0].HoraFechamento, Is.EqualTo(new TimeOnly(22, 0)));
    }

    [Test]
    public async Task H02_AlteracaoInvalidaPreservaFuncionamento()
    {
        //funcionamento valido ja cadastrado
        var repository = new HorarioRepositoryFake();
        var service = new HorarioFuncionamentoService(new QuadraRepositoryFake(), repository);
        var horario = await service.CadastrarAsync(1, 1, new TimeOnly(8, 0), new TimeOnly(22, 0));

        //tentar fechar antes de abrir
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.AlterarAsync(1, horario.IdHorario, 1, new TimeOnly(22, 0), new TimeOnly(8, 0));
        });

        //nenhuma alteracao foi salva e o horario anterior permanece
        var horarios = await service.ConsultarAsync(1);
        Assert.That(horarios, Has.Count.EqualTo(1));
        Assert.That(horarios[0].HoraAbertura, Is.EqualTo(new TimeOnly(8, 0)));
        Assert.That(horarios[0].HoraFechamento, Is.EqualTo(new TimeOnly(22, 0)));
        Assert.That(repository.Atualizacoes, Is.Zero);
    }

    private sealed class QuadraRepositoryFake : IQuadraRepository
    {
        public Task<Quadra?> ObterParaReservaAsync(int idQuadra, CancellationToken cancellationToken = default)
        {
            Quadra? quadra = idQuadra == 1 ? new Quadra { IdQuadra = 1 } : null;
            return Task.FromResult(quadra);
        }
    }

    private sealed class HorarioRepositoryFake : IHorarioFuncionamentoRepository
    {
        private readonly List<HorarioFuncionamento> _horarios = new();
        public int Atualizacoes { get; private set; }

        public Task<IReadOnlyList<HorarioFuncionamento>> ConsultarAsync(int idQuadra, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<HorarioFuncionamento> horarios = _horarios.Where(h => h.IdQuadra == idQuadra).ToList();
            return Task.FromResult(horarios);
        }

        public Task<HorarioFuncionamento?> ObterPorIdAsync(int idQuadra, int idHorario, CancellationToken cancellationToken = default)
        {
            var horario = _horarios.SingleOrDefault(h => h.IdQuadra == idQuadra && h.IdHorario == idHorario);
            return Task.FromResult(horario);
        }

        public Task AdicionarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default)
        {
            horario.IdHorario = _horarios.Count + 1;
            _horarios.Add(horario);
            return Task.CompletedTask;
        }

        public Task<bool> AtualizarAsync(HorarioFuncionamento horario, CancellationToken cancellationToken = default)
        {
            Atualizacoes++;
            return Task.FromResult(_horarios.Contains(horario));
        }
    }
}

