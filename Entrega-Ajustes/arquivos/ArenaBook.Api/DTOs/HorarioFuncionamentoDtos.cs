using System.ComponentModel.DataAnnotations;
using ArenaBook.Core.Entities;

namespace ArenaBook.Api.DTOs;

public record SalvarFuncionamentoRequest(
    [property: Range(1, 7)] int DiaSemana,
    [property: Required] TimeOnly? HoraAbertura,
    [property: Required] TimeOnly? HoraFechamento);

public record HorarioFuncionamentoResponse(int IdHorario, int IdQuadra, byte DiaSemana, TimeOnly HoraAbertura, TimeOnly HoraFechamento)
{
    public static HorarioFuncionamentoResponse De(
        HorarioFuncionamento horario) => new(
            horario.IdHorario,
            horario.IdQuadra,
            horario.DiaSemana,
            horario.HoraAbertura,
            horario.HoraFechamento);
}