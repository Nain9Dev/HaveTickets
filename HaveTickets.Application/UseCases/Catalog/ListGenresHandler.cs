using HaveTickets.Application.Abstractions;
using HaveTickets.Domain.Common;

namespace HaveTickets.Application.UseCases.Catalog;

public sealed class ListGenresHandler(ICatalogReadModel catalog)
{
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> genres = await catalog.ListGenresAsync(cancellationToken);
        return Result.Success(genres);
    }
}
