namespace ArenaBook.Core.Entities;

public class Quadra
{
    public int IdQuadra { get; set; }
    public string? Nome { get; set; }
    public decimal PrecoHora { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<QuadraEsporte> QuadraEsportes { get; set; } = new List<QuadraEsporte>();
    public ICollection<HorarioFuncionamento> HorariosFuncionamento { get; set; } = new List<HorarioFuncionamento>();
}

