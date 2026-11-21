using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;

namespace ArenaBook.Core.Services;

public class HorarioFuncionamentoService
{
    private readonly IQuadraRepository _quadraRepository;
    private readonly IHorarioFuncionamentoRepository _horarioRepository;

    public HorarioFuncionamentoService(IQuadraRepository quadraRepository, IHorarioFuncionamentoRepository horarioRepository)
    {
        _quadraRepository = quadraRepository;
        _horarioRepository = horarioRepository;
    }

    public async Task<IReadOnlyList<HorarioFuncionamento>> ConsultarAsync(int idQuadra, CancellationToken cancellationToken = default)
    {
        await ValidarQuadraAsync(idQuadra, cancellationToken);

        return await _horarioRepository.ConsultarAsync(idQuadra, cancellationToken);
    }

    public async Task<HorarioFuncionamento> CadastrarAsync(int idQuadra, int diaSemana, TimeOnly abertura, TimeOnly fechamento, CancellationToken cancellationToken = default)
    {
        ValidarIntervalo(diaSemana, abertura, fechamento);

        await ValidarQuadraAsync(idQuadra, cancellationToken);
        await ValidarDuplicidadeAsync(idQuadra, 0, diaSemana, abertura, cancellationToken);

        var horario = new HorarioFuncionamento
        {
            IdQuadra = idQuadra,
            DiaSemana = (byte)diaSemana,
            HoraAbertura = abertura,
            HoraFechamento = fechamento
        };

        await _horarioRepository.AdicionarAsync(horario, cancellationToken);

        return horario;
    }

    public async Task<HorarioFuncionamento> AlterarAsync(int idQuadra, int idHorario, int diaSemana, TimeOnly abertura, TimeOnly fechamento, CancellationToken cancellationToken = default)
    {
        ValidarIntervalo(diaSemana, abertura, fechamento);

        if (idHorario <= 0)
        {
            throw new ArgumentException("O horário deve ter identificador positivo.");
        }

        await ValidarQuadraAsync(idQuadra, cancellationToken);

        var horario = await _horarioRepository.ObterPorIdAsync(idQuadra, idHorario, cancellationToken);

        if (horario is null)
        {
            throw new KeyNotFoundException("Configuração de funcionamento não encontrada.");
        }

        await ValidarDuplicidadeAsync(idQuadra, idHorario, diaSemana, abertura, cancellationToken);

        //somente altera os dados depois de concluir todas as validações.
        horario.DiaSemana = (byte)diaSemana;
        horario.HoraAbertura = abertura;
        horario.HoraFechamento = fechamento;

        var atualizado = await _horarioRepository.AtualizarAsync(horario, cancellationToken);

        if (!atualizado)
        {
            throw new KeyNotFoundException("Configuração de funcionamento não encontrada.");
        }

        return horario;
    }

    private async Task ValidarQuadraAsync(int idQuadra, CancellationToken cancellationToken)
    {
        if (idQuadra <= 0)
        {
            throw new ArgumentException("A quadra deve ter identificador positivo.");
        }

        var quadra = await _quadraRepository.ObterParaReservaAsync(idQuadra, cancellationToken);

        if (quadra is null)
        {
            throw new KeyNotFoundException("Quadra não encontrada.");
        }
    }

    private static void ValidarIntervalo(int diaSemana, TimeOnly abertura, TimeOnly fechamento)
    {
        if (diaSemana < 1 || diaSemana > 7)
        {
            throw new ArgumentException("O dia da semana deve estar entre 1 e 7.");
        }

        if (fechamento <= abertura)
        {
            throw new ArgumentException("O fechamento deve ser posterior à abertura.");
        }

        if (abertura.Ticks % TimeSpan.TicksPerSecond != 0 || fechamento.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw new ArgumentException("Os horários devem ter precisão de segundos.");
        }
    }

    private async Task ValidarDuplicidadeAsync(int idQuadra, int idHorarioIgnorado, int diaSemana, TimeOnly abertura, CancellationToken cancellationToken)
    {
        var horarios = await _horarioRepository.ConsultarAsync(idQuadra, cancellationToken);

        var duplicado = horarios.Any(horario => horario.IdHorario != idHorarioIgnorado && horario.DiaSemana == diaSemana && horario.HoraAbertura == abertura);

        if (duplicado)
        {
            throw new ArgumentException("Já existe um funcionamento com essa abertura para a quadra e o dia informados.");
        }
    }
}
