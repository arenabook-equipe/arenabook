namespace ArenaBook.Core.Entities;

public class Cliente
{
    public int IdCliente { get; set; }
    public string? Nome { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

