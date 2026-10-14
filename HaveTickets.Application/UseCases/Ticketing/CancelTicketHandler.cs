using HaveTickets.Application.Abstractions;
using HaveTickets.Domain.Common;

namespace HaveTickets.Application.UseCases.Ticketing;

public sealed record CancelTicketCommand(Guid TicketId, Guid HolderId);

/// <summary>
/// Voids a ticket and releases its seat back onto the seat map. Cancelling twice
/// is not an error: the caller asked for a state, and that state already holds.
/// </summary>
public sealed class CancelTicketHandler(
    ITicketRepository tickets,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<Result> HandleAsync(
        CancelTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        TicketWithShowtime? found = await tickets.GetByIdAsync(command.TicketId, cancellationToken);
        if (found is null)
        {
            return DomainErrors.Ticket.NotFound(command.TicketId);
        }

        Result cancelled = found.Ticket.Cancel(found.Showtime, command.HolderId, clock.GetUtcNow());
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
