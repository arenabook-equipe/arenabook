using ArenaBook.Core.Entities;

namespace ArenaBook.Api.Contracts.Responses;

public record HorarioFuncionamentoResponse(int IdHorario, int IdQuadra, byte DiaSemana, TimeOnly HoraAbertura, TimeOnly HoraFechamento)
{
    public static HorarioFuncionamentoResponse CriarResposta(HorarioFuncionamento horario) => new(horario.IdHorario, horario.IdQuadra, horario.DiaSemana, horario.HoraAbertura, horario.HoraFechamento);
}
