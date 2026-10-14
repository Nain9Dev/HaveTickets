using HaveTickets.Api.Extensions;
using HaveTickets.Application.UseCases.Catalog;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;

namespace HaveTickets.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder group = routes
            .MapGroup("/api/v1")
            .WithTags("Catalog")
            .RequireRateLimiting(ServiceCollectionExtensions.ReadPolicy);

        group.MapGet("/movies", async (
                string? search,
                string? genre,
                int? page,
                int? pageSize,
                ListMoviesHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<PagedResponse<MovieSummaryResponse>> result = await handler.HandleAsync(
                    new ListMoviesQuery(search, genre, page ?? 1, pageSize ?? 12),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .WithName("ListMovies")
            .WithSummary("Lists the catalogue, optionally filtered by text and genre.")
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByQuery("search", "genre", "page", "pageSize"))
            .Produces<PagedResponse<MovieSummaryResponse>>();

        group.MapGet("/movies/{movieId:guid}", async (
                Guid movieId,
                GetMovieHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<MovieDetailResponse> result = await handler.HandleAsync(
                    new GetMovieQuery(movieId),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .WithName("GetMovie")
            .WithSummary("Returns one movie with every showtime still to come.")
            .Produces<MovieDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/genres", async (
                ListGenresHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<string>> result = await handler.HandleAsync(cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .WithName("ListGenres")
            .WithSummary("Returns every genre present in the catalogue.")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)))
            .Produces<IReadOnlyList<string>>();

        group.MapGet("/showtimes/{showtimeId:guid}/seat-map", async (
                Guid showtimeId,
                GetSeatMapHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<SeatMapResponse> result = await handler.HandleAsync(
                    new GetSeatMapQuery(showtimeId),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .WithName("GetSeatMap")
            .WithSummary("Returns the room layout and the seats already sold.")
            .WithDescription("Availability is a snapshot, never a reservation. The purchase endpoint is the only authority.")
            .Produces<SeatMapResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }
}
