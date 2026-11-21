using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;

namespace ArenaBook.Core.Services;

public class HorarioFuncionamentoService(IQuadraRepository quadraRepository, IHorarioFuncionamentoRepository repository)
{
    public async Task<IReadOnlyList<HorarioFuncionamento>> ConsultarAsync(int idQuadra, CancellationToken cancellationToken = default)
    {
        await ValidarQuadraAsync(idQuadra, cancellationToken);

        return await repository.ConsultarAsync(
            idQuadra, cancellationToken);
    }

    public async Task<HorarioFuncionamento> CadastrarAsync(int idQuadra, int diaSemana, TimeOnly abertura, TimeOnly fechamento, CancellationToken cancellationToken = default)
    {
        ValidarIntervalo(diaSemana, abertura, fechamento);

        await ValidarQuadraAsync(idQuadra, cancellationToken);

        await ValidarSobreposicaoAsync(idQuadra, null, diaSemana, abertura, fechamento, cancellationToken);

        var horario = new HorarioFuncionamento
        {
            IdQuadra = idQuadra,
            DiaSemana = (byte)diaSemana,
            HoraAbertura = abertura,
            HoraFechamento = fechamento
        };

        await repository.AdicionarAsync(horario, cancellationToken);

        return horario;
    }

    public async Task<HorarioFuncionamento> AlterarAsync(int idQuadra, int idHorario, int diaSemana, TimeOnly abertura, TimeOnly fechamento, CancellationToken cancellationToken = default)
    {
        // Todas as validações acontecem antes de modificar o registro.
        ValidarIntervalo(diaSemana, abertura, fechamento);

        if (idHorario <= 0)
        {
            throw new ArgumentException(
                "O horário deve ter identificador positivo.");
        }

        await ValidarQuadraAsync(idQuadra, cancellationToken);

        var horario = await repository.ObterAsync(idQuadra, idHorario, cancellationToken);

        if (horario is null)
        {
            throw new KeyNotFoundException("Configuração de funcionamento não encontrada.");
        }

        await ValidarSobreposicaoAsync(idQuadra, idHorario, diaSemana, abertura, fechamento, cancellationToken);

        horario.DiaSemana = (byte)diaSemana;
        horario.HoraAbertura = abertura;
        horario.HoraFechamento = fechamento;

        await repository.SalvarAsync(cancellationToken);

        return horario;
    }

    private static void ValidarIntervalo(int diaSemana, TimeOnly abertura, TimeOnly fechamento)
    {
        if (diaSemana is < 1 or > 7)
        {
            throw new ArgumentException("O dia da semana deve estar entre 1 e 7.");
        }

        if (fechamento <= abertura)
        {
            throw new ArgumentException("O fechamento deve ser posterior à abertura.");
        }

        if (abertura.Ticks % TimeSpan.TicksPerSecond != 0 ||
            fechamento.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw new ArgumentException("Os horários devem ter precisão de segundos.");
        }
    }

    private async Task ValidarQuadraAsync(int idQuadra, CancellationToken cancellationToken)
    {
        if (idQuadra <= 0)
        {
            throw new ArgumentException("A quadra deve ter identificador positivo.");
        }

        var quadra = await quadraRepository.ObterParaReservaAsync(idQuadra, cancellationToken);

        if (quadra is null)
        {
            throw new KeyNotFoundException("Quadra não encontrada.");
        }
    }

    private async Task ValidarSobreposicaoAsync(int idQuadra, int? idHorarioIgnorado, int diaSemana, TimeOnly abertura ,TimeOnly fechamento, CancellationToken cancellationToken)
    {
        var horarios = await repository.ConsultarAsync(
            idQuadra, cancellationToken);

        var sobreposto = horarios.Any(h =>
            h.IdHorario != idHorarioIgnorado &&
            h.DiaSemana == diaSemana &&
            h.HoraAbertura < fechamento &&
            h.HoraFechamento > abertura);

        if (sobreposto)
        {
            throw new ArgumentException("O intervalo se sobrepõe a um funcionamento já cadastrado.");
        }
    }
}