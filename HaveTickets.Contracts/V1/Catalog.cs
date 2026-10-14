namespace HaveTickets.Contracts.V1;

/// <summary>A movie as shown in the catalogue grid.</summary>
public sealed record MovieSummaryResponse(
    Guid Id,
    string Title,
    string Synopsis,
    string PosterUrl,
    int DurationMinutes,
    DateOnly ReleaseDate,
    string Genre,
    string AgeRating,
    int ShowtimeCount,
    DateTimeOffset? NextShowtimeAt);

/// <summary>A movie plus every showtime still on sale.</summary>
public sealed record MovieDetailResponse(
    Guid Id,
    string Title,
    string Synopsis,
    string PosterUrl,
    int DurationMinutes,
    DateOnly ReleaseDate,
    string Genre,
    string AgeRating,
    IReadOnlyList<ShowtimeResponse> Showtimes);

public sealed record ShowtimeResponse(
    Guid Id,
    DateTimeOffset StartsAt,
    decimal Price,
    string Currency,
    CinemaResponse Cinema,
    string AuditoriumName,
    int Capacity,
    int SeatsAvailable,
    bool SalesOpen);

public sealed record CinemaResponse(
    Guid Id,
    string Name,
    string City,
    string Address);

/// <summary>
/// Everything the seat picker needs in one round trip: the room layout, which
/// seats are gone, and the per-order cap enforced by the server.
/// </summary>
public sealed record SeatMapResponse(
    Guid ShowtimeId,
    string MovieTitle,
    string PosterUrl,
    DateTimeOffset StartsAt,
    string CinemaName,
    string AuditoriumName,
    int RowCount,
    int SeatsPerRow,
    decimal Price,
    string Currency,
    bool SalesOpen,
    int MaxSeatsPerOrder,
    IReadOnlyList<string> OccupiedSeats);
