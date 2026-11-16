using ArenaBook.Core.Enums;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Repositories;

public class ReservaRepository
{
    private readonly ArenaBookDbContext _context;

    public ReservaRepository(ArenaBookDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteSobreposicaoAsync(int idQuadra, DateOnly dataReserva, TimeOnly horaInicio, TimeOnly horaFim, IReadOnlyCollection<StatusReserva> statusBloqueantes, CancellationToken cancellationToken = default)
    {
        return _context.Reservas.AnyAsync(reserva =>
                reserva.IdQuadra == idQuadra &&
                reserva.DataReserva == dataReserva &&
                statusBloqueantes.Contains(reserva.IdStatus) &&
                reserva.HoraInicio < horaFim &&
                reserva.HoraFim > horaInicio, cancellationToken);
    }
}