namespace ArenaBook.Core.Entities;

public class HorarioFuncionamento
{
    public int IdHorario { get; set; }
    public int IdQuadra { get; set; }
    public byte DiaSemana { get; set; }
    public TimeOnly HoraAbertura { get; set; }
    public TimeOnly HoraFechamento { get; set; }
    public Quadra Quadra { get; set; } = null!;
}

