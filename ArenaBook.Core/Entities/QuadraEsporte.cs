namespace ArenaBook.Core.Entities;

public class QuadraEsporte
{
    public int IdQuadra { get; set; }
    public int IdEsporte { get; set; }
    public Quadra? Quadra { get; set; }
    public Esporte? Esporte { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

