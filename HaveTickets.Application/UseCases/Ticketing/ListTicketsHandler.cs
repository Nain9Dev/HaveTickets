using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;

namespace HaveTickets.Application.UseCases.Ticketing;

public sealed record ListTicketsQuery(Guid HolderId);

public sealed class ListTicketsHandler(ITicketReadModel tickets)
{
    public async Task<Result<IReadOnlyList<TicketResponse>>> HandleAsync(
        ListTicketsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.HolderId == Guid.Empty)
        {
            return DomainErrors.Order.HolderRequired;
        }

        IReadOnlyList<TicketResponse> result = await tickets.ListByHolderAsync(query.HolderId, cancellationToken);
        return Result.Success(result);
    }
}
