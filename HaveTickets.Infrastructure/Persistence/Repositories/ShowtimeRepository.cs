using HaveTickets.Application.Abstractions;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence.Repositories;

public sealed class ShowtimeRepository(HaveTicketsDbContext database) : IShowtimeRepository
{
    public async Task<ShowtimeWithVenue?> GetWithVenueAsync(
        Guid showtimeId,
        CancellationToken cancellationToken = default)
    {
        Showtime? showtime = await database.Showtimes
            .Include(candidate => candidate.Auditorium)
            .FirstOrDefaultAsync(candidate => candidate.Id == showtimeId, cancellationToken);

        return showtime?.Auditorium is null ? null : new ShowtimeWithVenue(showtime, showtime.Auditorium);
    }

    public async Task<IReadOnlySet<string>> GetOccupiedSeatLabelsAsync(
        Guid showtimeId,
        CancellationToken cancellationToken = default)
    {
        // Seat is value-converted, so the column already holds the label. Project
        // the value object and read Label in memory; SQL cannot see inside it.
        List<SeatNumber> seats = await database.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ShowtimeId == showtimeId && ticket.Status != TicketStatus.Cancelled)
            .Select(ticket => ticket.Seat)
            .ToListAsync(cancellationToken);

        return seats.Select(seat => seat.Label).ToHashSet(StringComparer.Ordinal);
    }
}
