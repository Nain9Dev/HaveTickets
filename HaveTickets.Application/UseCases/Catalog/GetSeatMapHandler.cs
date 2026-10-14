using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;

namespace HaveTickets.Application.UseCases.Catalog;

public sealed record GetSeatMapQuery(Guid ShowtimeId);

/// <summary>
/// Returns the room layout plus the seats already gone, so the client can render
/// the picker in a single request. The result is a snapshot: it is never trusted
/// as an availability guarantee, only as a hint. The database decides.
/// </summary>
public sealed class GetSeatMapHandler(ICatalogReadModel catalog, TimeProvider clock)
{
    public async Task<Result<SeatMapResponse>> HandleAsync(
        GetSeatMapQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SeatMapResponse? seatMap = await catalog.GetSeatMapAsync(
            query.ShowtimeId,
            clock.GetUtcNow(),
            cancellationToken);

        return seatMap is null
            ? DomainErrors.Showtime.NotFound(query.ShowtimeId)
            : seatMap;
    }
}
