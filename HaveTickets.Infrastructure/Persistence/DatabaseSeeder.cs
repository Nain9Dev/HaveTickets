using HaveTickets.Domain.Catalog;
using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.ValueObjects;
using HaveTickets.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence;

/// <summary>
/// Fills an empty database with a demo catalogue and keeps the schedule alive.
/// <para>
/// A portfolio deployment is looked at weeks or months after it was written, so a
/// one-shot seed of hard-coded dates would show nothing but past screenings. The
/// seeder therefore tops the schedule up on every start: it always guarantees
/// <see cref="ScheduleHorizonDays"/> days of upcoming showtimes, using
/// deterministic identifiers so re-entering it never duplicates a row.
/// </para>
/// </summary>
public static class DatabaseSeeder
{
    public const int ScheduleHorizonDays = 7;

    private static readonly int[] ShowtimeHoursUtc = [16, 19, 22];

    public static async Task SeedAsync(
        HaveTicketsDbContext database,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(clock);

        DateTimeOffset now = clock.GetUtcNow();

        await SeedCatalogueAsync(database, cancellationToken);
        await EnsureUpcomingShowtimesAsync(database, now, cancellationToken);
    }

    private static async Task SeedCatalogueAsync(
        HaveTicketsDbContext database,
        CancellationToken cancellationToken)
    {
        if (await database.Movies.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach ((string title, string synopsis, int minutes, string releaseDate, string genre, string rating) in MovieCatalogue)
        {
            Result<Movie> movie = Movie.Create(
                DeterministicId.From($"movie:{title}"),
                title,
                synopsis,
                posterUrl: string.Empty,
                minutes,
                DateOnly.Parse(releaseDate, System.Globalization.CultureInfo.InvariantCulture),
                genre,
                rating);

            if (movie.IsSuccess)
            {
                database.Movies.Add(movie.Value);
            }
        }

        foreach ((string name, string city, string address, (string room, int rows, int seats)[] rooms) in CinemaCatalogue)
        {
            Result<Cinema> cinema = Cinema.Create(
                DeterministicId.From($"cinema:{name}"),
                name,
                city,
                address,
                imageUrl: string.Empty);

            if (cinema.IsFailure)
            {
                continue;
            }

            foreach ((string room, int rows, int seats) in rooms)
            {
                cinema.Value.AddAuditorium(
                    DeterministicId.From($"auditorium:{name}:{room}"),
                    room,
                    rows,
                    seats);
            }

            database.Cinemas.Add(cinema.Value);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Guarantees a rolling window of upcoming screenings. Idempotent: the id of
    /// every slot is derived from (auditorium, date, hour), so a slot that already
    /// exists is skipped instead of being inserted twice.
    /// </summary>
    private static async Task EnsureUpcomingShowtimesAsync(
        HaveTicketsDbContext database,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        List<Guid> movieIds = await database.Movies
            .OrderBy(movie => movie.Title)
            .Select(movie => movie.Id)
            .ToListAsync(cancellationToken);

        List<Guid> auditoriumIds = await database.Auditoriums
            .OrderBy(auditorium => auditorium.CinemaId)
            .ThenBy(auditorium => auditorium.Name)
            .Select(auditorium => auditorium.Id)
            .ToListAsync(cancellationToken);

        if (movieIds.Count == 0 || auditoriumIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> existing = (await database.Showtimes
                .Where(showtime => showtime.StartsAt > now)
                .Select(showtime => showtime.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        DateOnly firstDay = DateOnly.FromDateTime(now.UtcDateTime);
        var added = new List<Showtime>();

        for (int dayOffset = 0; dayOffset < ScheduleHorizonDays; dayOffset++)
        {
            DateOnly day = firstDay.AddDays(dayOffset);

            for (int auditoriumIndex = 0; auditoriumIndex < auditoriumIds.Count; auditoriumIndex++)
            {
                for (int slot = 0; slot < ShowtimeHoursUtc.Length; slot++)
                {
                    var startsAt = new DateTimeOffset(
                        day.Year,
                        day.Month,
                        day.Day,
                        ShowtimeHoursUtc[slot],
                        0,
                        0,
                        TimeSpan.Zero);

                    if (startsAt <= now)
                    {
                        continue;
                    }

                    Guid auditoriumId = auditoriumIds[auditoriumIndex];
                    Guid showtimeId = DeterministicId.From($"showtime:{auditoriumId}:{day:yyyy-MM-dd}:{ShowtimeHoursUtc[slot]}");

                    if (!existing.Add(showtimeId))
                    {
                        continue;
                    }

                    Guid movieId = movieIds[(dayOffset + auditoriumIndex + slot) % movieIds.Count];
                    Result<Money> price = Money.Create(BasePrice + (auditoriumIndex * 0.75m) + (slot == 2 ? 1.50m : 0m));
                    Result<Showtime> showtime = Showtime.Create(
                        showtimeId,
                        movieId,
                        auditoriumId,
                        startsAt,
                        price.Value,
                        now);

                    if (showtime.IsSuccess)
                    {
                        added.Add(showtime.Value);
                    }
                }
            }
        }

        if (added.Count == 0)
        {
            return;
        }

        database.Showtimes.AddRange(added);
        await database.SaveChangesAsync(cancellationToken);
    }

    private const decimal BasePrice = 8.50m;

    /// <summary>
    /// Demo catalogue. Synopses are written for this repository; no marketing copy
    /// is reproduced. Posters are rendered procedurally by the client, which keeps
    /// the demo free of third-party image hosting.
    /// </summary>
    private static readonly (string Title, string Synopsis, int Minutes, string ReleaseDate, string Genre, string AgeRating)[] MovieCatalogue =
    [
        ("Dune: Part Two",
            "A dispossessed heir joins the desert people who took him in, and finds that the war he wanted becomes the war they need.",
            166, "2024-03-01", "Science fiction", "PG-13"),
        ("Oppenheimer",
            "The physicist who built the bomb spends the rest of his life arguing with the people who wanted to use it.",
            180, "2023-07-21", "Drama", "R"),
        ("Spider-Man: Across the Spider-Verse",
            "A teenager discovers that every version of his story ends the same way, and decides to break it.",
            140, "2023-06-02", "Animation", "PG"),
        ("Everything Everywhere All at Once",
            "A laundromat owner audits her taxes, her marriage and every life she never lived, in that order.",
            139, "2022-03-25", "Adventure", "R"),
        ("Blade Runner 2049",
            "A detective who hunts synthetic people starts pulling a thread that leads back to his own file.",
            164, "2017-10-06", "Science fiction", "R"),
        ("Parasite",
            "One family talks its way into another family's house, and both discover the basement.",
            132, "2019-05-30", "Thriller", "R"),
        ("Arrival",
            "A linguist is asked to translate a language with no tense, and learns what that costs.",
            116, "2016-11-11", "Science fiction", "PG-13"),
        ("The Matrix Resurrections",
            "A software designer keeps building the same game, and cannot remember why it feels like a memory.",
            148, "2021-12-22", "Science fiction", "R")
    ];

    private static readonly (string Name, string City, string Address, (string Room, int Rows, int Seats)[] Rooms)[] CinemaCatalogue =
    [
        ("Aurora Cinema", "Barcelona", "Carrer de la Llum 12",
            [("Sala 1", 8, 12), ("Sala 2", 6, 10)]),
        ("Nova Screens", "Madrid", "Calle del Proyector 45",
            [("Sala Premium", 10, 14), ("Sala 3", 7, 12)]),
        ("Lumiere Filmhouse", "Valencia", "Avinguda del Cinema 8",
            [("Sala A", 6, 9), ("Sala B", 5, 8)])
    ];
}
