namespace ArenaBook.Core.Entities;

public class Esporte
{
    public int IdEsporte { get; set; }
    public string? Nome { get; set; }
    public ICollection<QuadraEsporte> QuadraEsportes { get; set; } = new List<QuadraEsporte>();
}

