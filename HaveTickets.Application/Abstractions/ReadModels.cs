using HaveTickets.Contracts.V1;

namespace HaveTickets.Application.Abstractions;

/// <summary>Filters applied to the catalogue listing.</summary>
public sealed record MovieCatalogQuery(string? Search, string? Genre, int Page, int PageSize);

/// <summary>
/// Read side of the catalogue. Queries project straight into the wire contract:
/// there is no aggregate to rehydrate when nothing is going to be modified.
/// </summary>
public interface ICatalogReadModel
{
    Task<PagedResponse<MovieSummaryResponse>> ListMoviesAsync(
        MovieCatalogQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<MovieDetailResponse?> GetMovieAsync(
        Guid movieId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<SeatMapResponse?> GetSeatMapAsync(
        Guid showtimeId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListGenresAsync(CancellationToken cancellationToken = default);
}

/// <summary>Read side of ticketing.</summary>
public interface ITicketReadModel
{
    Task<IReadOnlyList<TicketResponse>> ListByHolderAsync(
        Guid holderId,
        CancellationToken cancellationToken = default);

    Task<TicketResponse?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketResponse?> GetTicketByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
