using ArenaBook.Core.Enums;

namespace ArenaBook.Core.Entities;

public class Reserva
{
    public int IdReserva { get; set; }
    public int IdQuadra { get; set; }
    public int IdEsporte { get; set; }
    public int IdCliente { get; set; }
    public DateOnly DataReserva { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public decimal ValorTotal { get; set; }
    public StatusReserva IdStatus { get; set; } = StatusReserva.Pendente;
    public DateTime DataCriacao { get; set; }
    public Cliente? Cliente { get; set; }
    public QuadraEsporte? QuadraEsporte { get; set; }
}

