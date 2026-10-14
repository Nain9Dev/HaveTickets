using HaveTickets.Application.Abstractions;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository(HaveTicketsDbContext database) : ITicketRepository
{
    public async Task<TicketWithShowtime?> GetByIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        Ticket? ticket = await database.Tickets
            .Include(candidate => candidate.Showtime)
            .FirstOrDefaultAsync(candidate => candidate.Id == ticketId, cancellationToken);

        return Compose(ticket);
    }

    public async Task<TicketWithShowtime?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        ConfirmationCode target = ConfirmationCode.FromStorage(code);

        Ticket? ticket = await database.Tickets
            .Include(candidate => candidate.Showtime)
            .FirstOrDefaultAsync(candidate => candidate.Code == target, cancellationToken);

        return Compose(ticket);
    }

    private static TicketWithShowtime? Compose(Ticket? ticket) =>
        ticket?.Showtime is null ? null : new TicketWithShowtime(ticket, ticket.Showtime);
}
