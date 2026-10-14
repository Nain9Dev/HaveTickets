using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.ValueObjects;

namespace HaveTickets.Domain.Ticketing;

/// <summary>
/// One seat sold for one showtime. Created only through <see cref="Order.Place"/>,
/// so a ticket cannot exist without the order-level rules having been satisfied.
/// </summary>
public sealed class Ticket : Entity
{
    private Ticket(
        Guid id,
        Guid orderId,
        Guid showtimeId,
        Guid holderId,
        SeatNumber seat,
        Money pricePaid,
        ConfirmationCode code,
        DateTimeOffset issuedAt)
        : base(id)
    {
        OrderId = orderId;
        ShowtimeId = showtimeId;
        HolderId = holderId;
        Seat = seat;
        PricePaid = pricePaid;
        Code = code;
        IssuedAt = issuedAt;
        Status = TicketStatus.Issued;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private Ticket()
    {
        Seat = SeatNumber.Create(1, 1).Value;
        PricePaid = Money.Zero();
        Code = ConfirmationCode.FromStorage("00000000");
    }

    public Guid OrderId { get; private set; }

    public Order? Order { get; private set; }

    public Guid ShowtimeId { get; private set; }

    public Showtime? Showtime { get; private set; }

    /// <summary>Anonymous holder identity, issued by the client and stored locally.</summary>
    public Guid HolderId { get; private set; }

    public SeatNumber Seat { get; private set; }

    /// <summary>Price snapshot taken at purchase time; later price changes do not apply.</summary>
    public Money PricePaid { get; private set; }

    /// <summary>The single identifier printed on the ticket and encoded in its QR code.</summary>
    public ConfirmationCode Code { get; private set; }

    public TicketStatus Status { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    internal static Ticket Issue(
        Guid orderId,
        Guid showtimeId,
        Guid holderId,
        SeatNumber seat,
        Money pricePaid,
        DateTimeOffset issuedAt) =>
        new(Guid.CreateVersion7(), orderId, showtimeId, holderId, seat, pricePaid with { }, ConfirmationCode.New(), issuedAt);

    /// <summary>
    /// Redeems the ticket at the door. Single use: a second attempt fails, which is
    /// what stops one screenshot of a QR code from admitting a whole group.
    /// </summary>
    public Result CheckIn(Showtime showtime, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(showtime);

        if (Status == TicketStatus.Cancelled)
        {
            return DomainErrors.Ticket.Cancelled;
        }

        if (Status == TicketStatus.CheckedIn)
        {
            return DomainErrors.Ticket.AlreadyCheckedIn;
        }

        if (now < showtime.CheckInOpensAt)
        {
            return DomainErrors.Ticket.CheckInNotOpenYet;
        }

        if (now > showtime.CheckInClosesAt)
        {
            return DomainErrors.Ticket.CheckInClosed;
        }

        Status = TicketStatus.CheckedIn;
        CheckedInAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Voids the ticket and releases its seat. Idempotent: cancelling an already
    /// cancelled ticket succeeds without changing anything.
    /// </summary>
    public Result Cancel(Showtime showtime, Guid requestedBy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(showtime);

        if (HolderId != requestedBy)
        {
            return DomainErrors.Ticket.NotOwnedByHolder;
        }

        if (Status == TicketStatus.Cancelled)
        {
            return Result.Success();
        }

        if (Status == TicketStatus.CheckedIn)
        {
            return DomainErrors.Ticket.AlreadyCheckedIn;
        }

        if (!showtime.IsOpenForSale(now))
        {
            return DomainErrors.Ticket.TooLateToCancel;
        }

        Status = TicketStatus.Cancelled;
        CancelledAt = now;
        return Result.Success();
    }

    /// <summary>A ticket occupies its seat unless it was cancelled.</summary>
    public bool OccupiesSeat => Status != TicketStatus.Cancelled;
}
