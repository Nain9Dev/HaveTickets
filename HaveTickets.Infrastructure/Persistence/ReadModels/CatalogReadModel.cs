using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Catalog;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Catalogue queries.
/// <para>
/// Every query projects to flat primitives in SQL and shapes the response in
/// memory. Money in particular is read as integer minor units: pushing a decimal
/// division into SQLite would turn exact cents into a floating point number.
/// </para>
/// </summary>
public sealed class CatalogReadModel(HaveTicketsDbContext database) : ICatalogReadModel
{
    private const decimal MinorUnitsPerUnit = 100m;

    public async Task<PagedResponse<MovieSummaryResponse>> ListMoviesAsync(
        MovieCatalogQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Movie> movies = database.Movies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            movies = movies.Where(movie =>
                EF.Functions.Like(movie.Title, pattern) ||
                EF.Functions.Like(movie.Synopsis, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            movies = movies.Where(movie => movie.Genre == query.Genre);
        }

        int totalCount = await movies.CountAsync(cancellationToken);

        List<MovieSummaryResponse> items = await movies
            .OrderBy(movie => movie.Title)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(movie => new MovieSummaryResponse(
                movie.Id,
                movie.Title,
                movie.Synopsis,
                movie.PosterUrl,
                movie.DurationMinutes,
                movie.ReleaseDate,
                movie.Genre,
                movie.AgeRating,
                movie.Showtimes.Count(showtime => showtime.StartsAt > now),
                movie.Showtimes
                    .Where(showtime => showtime.StartsAt > now)
                    .OrderBy(showtime => showtime.StartsAt)
                    .Select(showtime => (DateTimeOffset?)showtime.StartsAt)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResponse<MovieSummaryResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<MovieDetailResponse?> GetMovieAsync(
        Guid movieId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        MovieHeader? header = await database.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => new MovieHeader(
                movie.Id,
                movie.Title,
                movie.Synopsis,
                movie.PosterUrl,
                movie.DurationMinutes,
                movie.ReleaseDate,
                movie.Genre,
                movie.AgeRating))
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        List<ShowtimeRow> rows = await database.Showtimes
            .AsNoTracking()
            .Where(showtime => showtime.MovieId == movieId && showtime.StartsAt > now)
            .OrderBy(showtime => showtime.StartsAt)
            .Select(showtime => new ShowtimeRow(
                showtime.Id,
                showtime.StartsAt,
                showtime.Price.AmountInMinorUnits,
                showtime.Price.Currency,
                showtime.Auditorium!.Cinema!.Id,
                showtime.Auditorium!.Cinema!.Name,
                showtime.Auditorium!.Cinema!.City,
                showtime.Auditorium!.Cinema!.Address,
                showtime.Auditorium!.Name,
                showtime.Auditorium!.RowCount,
                showtime.Auditorium!.SeatsPerRow,
                showtime.Tickets.Count(ticket => ticket.Status != TicketStatus.Cancelled)))
            .ToListAsync(cancellationToken);

        return new MovieDetailResponse(
            header.Id,
            header.Title,
            header.Synopsis,
            header.PosterUrl,
            header.DurationMinutes,
            header.ReleaseDate,
            header.Genre,
            header.AgeRating,
            rows.Select(row => ToShowtimeResponse(row, now)).ToList());
    }

    public async Task<SeatMapResponse?> GetSeatMapAsync(
        Guid showtimeId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        SeatMapHeader? header = await database.Showtimes
            .AsNoTracking()
            .Where(showtime => showtime.Id == showtimeId)
            .Select(showtime => new SeatMapHeader(
                showtime.Id,
                showtime.StartsAt,
                showtime.Price.AmountInMinorUnits,
                showtime.Price.Currency,
                showtime.Movie!.Title,
                showtime.Movie!.PosterUrl,
                showtime.Auditorium!.Cinema!.Name,
                showtime.Auditorium!.Name,
                showtime.Auditorium!.RowCount,
                showtime.Auditorium!.SeatsPerRow))
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        List<SeatNumber> occupied = await database.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ShowtimeId == showtimeId && ticket.Status != TicketStatus.Cancelled)
            .Select(ticket => ticket.Seat)
            .ToListAsync(cancellationToken);

        return new SeatMapResponse(
            header.Id,
            header.MovieTitle,
            header.PosterUrl,
            header.StartsAt,
            header.CinemaName,
            header.AuditoriumName,
            header.RowCount,
            header.SeatsPerRow,
            header.PriceAmountMinorUnits / MinorUnitsPerUnit,
            header.PriceCurrency,
            IsOpenForSale(header.StartsAt, now),
            Order.MaxSeatsPerOrder,
            occupied.Select(seat => seat.Label).Order(StringComparer.Ordinal).ToList());
    }

    public async Task<IReadOnlyList<string>> ListGenresAsync(CancellationToken cancellationToken = default)
    {
        List<string> genres = await database.Movies
            .AsNoTracking()
            .Where(movie => movie.Genre != string.Empty)
            .Select(movie => movie.Genre)
            .Distinct()
            .OrderBy(genre => genre)
            .ToListAsync(cancellationToken);

        return genres;
    }

    private static bool IsOpenForSale(DateTimeOffset startsAt, DateTimeOffset now) =>
        now < startsAt - Showtime.SalesCloseLeadTime;

    private static ShowtimeResponse ToShowtimeResponse(ShowtimeRow row, DateTimeOffset now)
    {
        int capacity = row.RowCount * row.SeatsPerRow;

        return new ShowtimeResponse(
            row.Id,
            row.StartsAt,
            row.PriceAmountMinorUnits / MinorUnitsPerUnit,
            row.PriceCurrency,
            new CinemaResponse(row.CinemaId, row.CinemaName, row.CinemaCity, row.CinemaAddress),
            row.AuditoriumName,
            capacity,
            Math.Max(0, capacity - row.SeatsSold),
            IsOpenForSale(row.StartsAt, now));
    }

    private sealed record MovieHeader(
        Guid Id,
        string Title,
        string Synopsis,
        string PosterUrl,
        int DurationMinutes,
        DateOnly ReleaseDate,
        string Genre,
        string AgeRating);

    private sealed record ShowtimeRow(
        Guid Id,
        DateTimeOffset StartsAt,
        long PriceAmountMinorUnits,
        string PriceCurrency,
        Guid CinemaId,
        string CinemaName,
        string CinemaCity,
        string CinemaAddress,
        string AuditoriumName,
        int RowCount,
        int SeatsPerRow,
        int SeatsSold);

    private sealed record SeatMapHeader(
        Guid Id,
        DateTimeOffset StartsAt,
        long PriceAmountMinorUnits,
        string PriceCurrency,
        string MovieTitle,
        string PosterUrl,
        string CinemaName,
        string AuditoriumName,
        int RowCount,
        int SeatsPerRow);
}
