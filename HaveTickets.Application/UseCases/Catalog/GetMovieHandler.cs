using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;

namespace HaveTickets.Application.UseCases.Catalog;

public sealed record GetMovieQuery(Guid MovieId);

public sealed class GetMovieHandler(ICatalogReadModel catalog, TimeProvider clock)
{
    public async Task<Result<MovieDetailResponse>> HandleAsync(
        GetMovieQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        MovieDetailResponse? movie = await catalog.GetMovieAsync(
            query.MovieId,
            clock.GetUtcNow(),
            cancellationToken);

        return movie is null
            ? DomainErrors.Movie.NotFound(query.MovieId)
            : movie;
    }
}
