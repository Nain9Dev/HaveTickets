using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;

namespace HaveTickets.Domain.Catalog;

/// <summary>A film that can be scheduled into showtimes.</summary>
public sealed class Movie : Entity
{
    public const int MaxDurationMinutes = 600;

    private readonly List<Showtime> _showtimes = [];

    private Movie(
        Guid id,
        string title,
        string synopsis,
        string posterUrl,
        int durationMinutes,
        DateOnly releaseDate,
        string genre,
        string ageRating)
        : base(id)
    {
        Title = title;
        Synopsis = synopsis;
        PosterUrl = posterUrl;
        DurationMinutes = durationMinutes;
        ReleaseDate = releaseDate;
        Genre = genre;
        AgeRating = ageRating;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private Movie()
    {
        Title = string.Empty;
        Synopsis = string.Empty;
        PosterUrl = string.Empty;
        Genre = string.Empty;
        AgeRating = string.Empty;
    }

    public string Title { get; private set; }

    public string Synopsis { get; private set; }

    public string PosterUrl { get; private set; }

    public int DurationMinutes { get; private set; }

    public DateOnly ReleaseDate { get; private set; }

    public string Genre { get; private set; }

    /// <summary>Content rating shown next to the title, for example PG-13.</summary>
    public string AgeRating { get; private set; }

    public IReadOnlyCollection<Showtime> Showtimes => _showtimes.AsReadOnly();

    public static Result<Movie> Create(
        Guid id,
        string title,
        string synopsis,
        string posterUrl,
        int durationMinutes,
        DateOnly releaseDate,
        string genre,
        string ageRating)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return DomainErrors.Movie.TitleRequired;
        }

        if (durationMinutes is < 1 or > MaxDurationMinutes)
        {
            return DomainErrors.Movie.InvalidDuration;
        }

        return new Movie(
            id,
            title.Trim(),
            synopsis?.Trim() ?? string.Empty,
            posterUrl?.Trim() ?? string.Empty,
            durationMinutes,
            releaseDate,
            genre?.Trim() ?? string.Empty,
            ageRating?.Trim() ?? string.Empty);
    }
}
