using HaveTickets.Domain.Catalog;
using HaveTickets.Domain.Common;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;
using HaveTickets.Domain.Venues;

namespace HaveTickets.Domain.Scheduling;

/// <summary>
/// A single screening of a movie in an auditorium at a point in time.
/// <para>
/// The showtime owns the sales and check-in windows. Every time-dependent rule in
/// the system is expressed here as a pure function of an injected instant, never
/// of <c>DateTime.UtcNow</c>, so the rules are deterministic under test.
/// </para>
/// </summary>
public sealed class Showtime : Entity
{
    /// <summary>Sales stop shortly before the film starts.</summary>
    public static readonly TimeSpan SalesCloseLeadTime = TimeSpan.FromMinutes(15);

    /// <summary>Doors open one hour before the film starts.</summary>
    public static readonly TimeSpan CheckInOpensLeadTime = TimeSpan.FromMinutes(60);

    /// <summary>Latecomers are admitted for half an hour after the start.</summary>
    public static readonly TimeSpan CheckInGracePeriod = TimeSpan.FromMinutes(30);

    private readonly List<Ticket> _tickets = [];

    private Showtime(Guid id, Guid movieId, Guid auditoriumId, DateTimeOffset startsAt, Money price)
        : base(id)
    {
        MovieId = movieId;
        AuditoriumId = auditoriumId;
        StartsAt = startsAt;
        Price = price;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private Showtime() => Price = Money.Zero();

    public Guid MovieId { get; private set; }

    public Movie? Movie { get; private set; }

    public Guid AuditoriumId { get; private set; }

    public Auditorium? Auditorium { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public Money Price { get; private set; }

    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    public DateTimeOffset SalesCloseAt => StartsAt - SalesCloseLeadTime;

    public DateTimeOffset CheckInOpensAt => StartsAt - CheckInOpensLeadTime;

    public DateTimeOffset CheckInClosesAt => StartsAt + CheckInGracePeriod;

    public static Result<Showtime> Create(
        Guid id,
        Guid movieId,
        Guid auditoriumId,
        DateTimeOffset startsAt,
        Money price,
        DateTimeOffset now)
    {
        if (startsAt <= now)
        {
            return DomainErrors.Showtime.StartsInThePast;
        }

        return new Showtime(id, movieId, auditoriumId, startsAt.ToUniversalTime(), price);
    }

    /// <summary>Rebuilds a showtime without the future-dated rule. Seeding and tests only.</summary>
    public static Showtime Reconstitute(Guid id, Guid movieId, Guid auditoriumId, DateTimeOffset startsAt, Money price) =>
        new(id, movieId, auditoriumId, startsAt.ToUniversalTime(), price);

    public bool IsOpenForSale(DateTimeOffset now) => now < SalesCloseAt;

    public bool IsCheckInOpen(DateTimeOffset now) => now >= CheckInOpensAt && now <= CheckInClosesAt;
}
