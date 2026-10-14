using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.ValueObjects;
using HaveTickets.Domain.Venues;

namespace HaveTickets.Domain.Ticketing;

/// <summary>
/// A purchase of one or more seats for a single showtime. This is the aggregate
/// root for the write side of ticketing: every seat rule is enforced here, and
/// tickets are only ever produced by <see cref="Place"/>.
/// </summary>
public sealed class Order : Entity
{
    /// <summary>Per-order seat cap, the usual defence against bulk scalping.</summary>
    public const int MaxSeatsPerOrder = 6;

    private readonly List<Ticket> _tickets = [];

    private Order(
        Guid id,
        Guid holderId,
        Guid showtimeId,
        ConfirmationCode reference,
        Money total,
        DateTimeOffset placedAt)
        : base(id)
    {
        HolderId = holderId;
        ShowtimeId = showtimeId;
        Reference = reference;
        Total = total;
        PlacedAt = placedAt;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private Order()
    {
        Reference = ConfirmationCode.FromStorage("00000000");
        Total = Money.Zero();
    }

    public Guid HolderId { get; private set; }

    public Guid ShowtimeId { get; private set; }

    public Showtime? Showtime { get; private set; }

    /// <summary>Booking reference for the whole purchase.</summary>
    public ConfirmationCode Reference { get; private set; }

    /// <summary>Sum of the ticket prices at the moment of purchase.</summary>
    public Money Total { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    /// <summary>
    /// Places an order, applying every rule in a fixed order so the same input
    /// always produces the same error:
    /// <list type="number">
    ///   <item>the holder must be identified;</item>
    ///   <item>the showtime must belong to the given auditorium;</item>
    ///   <item>sales for the showtime must still be open;</item>
    ///   <item>between 1 and <see cref="MaxSeatsPerOrder"/> seats;</item>
    ///   <item>no seat repeated within the order;</item>
    ///   <item>every seat must exist in the auditorium;</item>
    ///   <item>no seat may already be occupied.</item>
    /// </list>
    /// <para>
    /// The occupancy check here is advisory: it produces a good error message from
    /// a recent read. The authoritative guarantee is the unique index on
    /// (ShowtimeId, Seat) in the database, which is what makes two concurrent
    /// buyers of the same seat resolve to exactly one winner.
    /// </para>
    /// </summary>
    public static Result<Order> Place(
        Showtime showtime,
        Auditorium auditorium,
        Guid holderId,
        IReadOnlyList<SeatNumber> seats,
        IReadOnlySet<string> occupiedSeatLabels,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(showtime);
        ArgumentNullException.ThrowIfNull(auditorium);
        ArgumentNullException.ThrowIfNull(seats);
        ArgumentNullException.ThrowIfNull(occupiedSeatLabels);

        if (holderId == Guid.Empty)
        {
            return DomainErrors.Order.HolderRequired;
        }

        if (showtime.AuditoriumId != auditorium.Id)
        {
            return Error.Unexpected(
                "order.auditorium_mismatch",
                "The auditorium provided does not host this showtime.");
        }

        if (!showtime.IsOpenForSale(now))
        {
            return DomainErrors.Showtime.SalesClosed;
        }

        if (seats.Count == 0)
        {
            return DomainErrors.Order.NoSeatsSelected;
        }

        if (seats.Count > MaxSeatsPerOrder)
        {
            return DomainErrors.Order.TooManySeats(MaxSeatsPerOrder);
        }

        var seen = new HashSet<string>(seats.Count, StringComparer.Ordinal);
        foreach (SeatNumber seat in seats)
        {
            if (!seen.Add(seat.Label))
            {
                return DomainErrors.Seat.Duplicated(seat.Label);
            }
        }

        foreach (SeatNumber seat in seats)
        {
            if (!auditorium.Contains(seat))
            {
                return DomainErrors.Seat.NotInAuditorium(seat.Label);
            }
        }

        foreach (SeatNumber seat in seats)
        {
            if (occupiedSeatLabels.Contains(seat.Label))
            {
                return DomainErrors.Seat.AlreadyTaken(seat.Label);
            }
        }

        Money total = showtime.Price.Multiply(seats.Count);
        var order = new Order(
            Guid.CreateVersion7(),
            holderId,
            showtime.Id,
            ConfirmationCode.New(),
            total,
            now);

        foreach (SeatNumber seat in seats.OrderBy(seat => seat))
        {
            order._tickets.Add(Ticket.Issue(order.Id, showtime.Id, holderId, seat, showtime.Price, now));
        }

        return order;
    }
}
