using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.Venues;

namespace HaveTickets.Application.Abstractions;

/// <summary>A showtime together with the room that hosts it.</summary>
public sealed record ShowtimeWithVenue(Showtime Showtime, Auditorium Auditorium);

/// <summary>A ticket together with the showtime it grants access to.</summary>
public sealed record TicketWithShowtime(Ticket Ticket, Showtime Showtime);

public interface IShowtimeRepository
{
    Task<ShowtimeWithVenue?> GetWithVenueAsync(Guid showtimeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Canonical labels of every seat currently held for the showtime. Cancelled
    /// tickets release their seat and are therefore excluded.
    /// </summary>
    Task<IReadOnlySet<string>> GetOccupiedSeatLabelsAsync(Guid showtimeId, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    void Add(Order order);
}

public interface ITicketRepository
{
    Task<TicketWithShowtime?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketWithShowtime?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// Commits the unit of work.
/// <para>
/// Returns a failed <see cref="Result"/> instead of throwing when the database
/// rejects a write for a reason the domain already models, above all the unique
/// index that guarantees one seat is sold at most once.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
