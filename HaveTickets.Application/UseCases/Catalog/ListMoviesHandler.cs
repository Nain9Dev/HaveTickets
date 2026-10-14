using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;

namespace HaveTickets.Application.UseCases.Catalog;

/// <summary>
/// Page through the catalogue, optionally narrowed by free text and genre.
/// Paging arguments are clamped rather than rejected: a bad page number is a
/// client bug, not something a visitor should see as an error page.
/// </summary>
public sealed record ListMoviesQuery(string? Search = null, string? Genre = null, int Page = 1, int PageSize = 12)
{
    public const int MaxPageSize = 48;

    public MovieCatalogQuery Normalise() => new(
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        string.IsNullOrWhiteSpace(Genre) ? null : Genre.Trim(),
        Math.Max(1, Page),
        Math.Clamp(PageSize, 1, MaxPageSize));
}

public sealed class ListMoviesHandler(ICatalogReadModel catalog, TimeProvider clock)
{
    public async Task<Result<PagedResponse<MovieSummaryResponse>>> HandleAsync(
        ListMoviesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        PagedResponse<MovieSummaryResponse> page = await catalog.ListMoviesAsync(
            query.Normalise(),
            clock.GetUtcNow(),
            cancellationToken);

        return page;
    }
}
