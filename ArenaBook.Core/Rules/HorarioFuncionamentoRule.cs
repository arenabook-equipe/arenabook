
namespace ArenaBook.Core.Rules;

public static class HorarioFuncionamentoRule
{
    public static bool EstaDentroDoFuncionamento(
        DateOnly dataReserva,
        TimeOnly horaInicio,
        TimeOnly horaFim)
    {
        // Verifica se o horário final é posterior ao inicial.
        if (horaFim <= horaInicio)
            return false;

        // Horário de abertura.
        TimeOnly abertura = new TimeOnly(8, 0);

        // Define o fechamento conforme o dia da semana.
        TimeOnly fechamento =
            dataReserva.DayOfWeek == DayOfWeek.Saturday ||
            dataReserva.DayOfWeek == DayOfWeek.Sunday
                ? new TimeOnly(16, 0)
                : new TimeOnly(22, 0);

        // Verifica se a reserva respeita o funcionamento.
        return horaInicio >= abertura &&
               horaFim <= fechamento;
    }
}
