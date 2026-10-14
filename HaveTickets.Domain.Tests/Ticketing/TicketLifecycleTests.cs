using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.Venues;
using Shouldly;

namespace HaveTickets.Domain.Tests.Ticketing;

public sealed class TicketLifecycleTests
{
    private readonly Auditorium _auditorium = Fixtures.CreateAuditorium();
    private readonly Showtime _showtime;

    public TicketLifecycleTests()
    {
        _showtime = Fixtures.CreateShowtime(_auditorium, TimeSpan.FromDays(1));
    }

    private Ticket IssueTicket() =>
        Order.Place(
                _showtime,
                _auditorium,
                Fixtures.Holder,
                Fixtures.Seats("A1"),
                Fixtures.Occupied(),
                Fixtures.Now)
            .Value.Tickets.Single();

    [Fact]
    public void A_new_ticket_is_issued_and_holds_its_seat()
    {
        Ticket ticket = IssueTicket();

        ticket.Status.ShouldBe(TicketStatus.Issued);
        ticket.OccupiesSeat.ShouldBeTrue();
        ticket.CheckedInAt.ShouldBeNull();
        ticket.CancelledAt.ShouldBeNull();
    }

    [Fact]
    public void Check_in_is_refused_before_the_window_opens()
    {
        Ticket ticket = IssueTicket();

        ticket.CheckIn(_showtime, _showtime.CheckInOpensAt - TimeSpan.FromSeconds(1))
            .ShouldFailWith("ticket.check_in_not_open_yet");
        ticket.Status.ShouldBe(TicketStatus.Issued);
    }

    [Fact]
    public void Check_in_opens_exactly_when_the_window_starts()
    {
        Ticket ticket = IssueTicket();

        ticket.CheckIn(_showtime, _showtime.CheckInOpensAt).ShouldSucceed();
        ticket.Status.ShouldBe(TicketStatus.CheckedIn);
        ticket.CheckedInAt.ShouldBe(_showtime.CheckInOpensAt);
    }

    [Fact]
    public void Check_in_is_still_accepted_at_the_end_of_the_grace_period()
    {
        IssueTicket().CheckIn(_showtime, _showtime.CheckInClosesAt).ShouldSucceed();
    }

    [Fact]
    public void Check_in_is_refused_after_the_grace_period()
    {
        IssueTicket().CheckIn(_showtime, _showtime.CheckInClosesAt + TimeSpan.FromSeconds(1))
            .ShouldFailWith("ticket.check_in_closed");
    }

    [Fact]
    public void A_ticket_cannot_be_used_twice()
    {
        Ticket ticket = IssueTicket();
        ticket.CheckIn(_showtime, _showtime.StartsAt).ShouldSucceed();

        ticket.CheckIn(_showtime, _showtime.StartsAt).ShouldFailWith("ticket.already_checked_in");
    }

    [Fact]
    public void A_cancelled_ticket_cannot_be_used()
    {
        Ticket ticket = IssueTicket();
        ticket.Cancel(_showtime, Fixtures.Holder, Fixtures.Now).ShouldSucceed();

        ticket.CheckIn(_showtime, _showtime.StartsAt).ShouldFailWith("ticket.cancelled");
    }

    [Fact]
    public void Cancel_releases_the_seat()
    {
        Ticket ticket = IssueTicket();

        ticket.Cancel(_showtime, Fixtures.Holder, Fixtures.Now).ShouldSucceed();

        ticket.Status.ShouldBe(TicketStatus.Cancelled);
        ticket.OccupiesSeat.ShouldBeFalse();
        ticket.CancelledAt.ShouldBe(Fixtures.Now);
    }

    [Fact]
    public void Cancel_is_idempotent_and_keeps_the_first_timestamp()
    {
        Ticket ticket = IssueTicket();
        ticket.Cancel(_showtime, Fixtures.Holder, Fixtures.Now).ShouldSucceed();

        ticket.Cancel(_showtime, Fixtures.Holder, Fixtures.Now + TimeSpan.FromHours(1)).ShouldSucceed();

        ticket.CancelledAt.ShouldBe(Fixtures.Now);
    }

    [Fact]
    public void Cancel_by_another_holder_is_forbidden()
    {
        Ticket ticket = IssueTicket();

        Result result = ticket.Cancel(_showtime, Fixtures.OtherHolder, Fixtures.Now);

        result.ShouldFailWith("ticket.not_owned_by_holder");
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        ticket.Status.ShouldBe(TicketStatus.Issued);
    }

    [Fact]
    public void Cancel_is_refused_once_sales_have_closed()
    {
        Ticket ticket = IssueTicket();

        ticket.Cancel(_showtime, Fixtures.Holder, _showtime.SalesCloseAt)
            .ShouldFailWith("ticket.too_late_to_cancel");
    }

    [Fact]
    public void A_used_ticket_cannot_be_cancelled()
    {
        Ticket ticket = IssueTicket();
        ticket.CheckIn(_showtime, _showtime.StartsAt).ShouldSucceed();

        ticket.Cancel(_showtime, Fixtures.Holder, Fixtures.Now).ShouldFailWith("ticket.already_checked_in");
    }

    [Fact]
    public void Ownership_is_checked_before_the_state_of_the_ticket()
    {
        // A stranger must not learn whether somebody else's ticket was already used.
        Ticket ticket = IssueTicket();
        ticket.CheckIn(_showtime, _showtime.StartsAt).ShouldSucceed();

        ticket.Cancel(_showtime, Fixtures.OtherHolder, Fixtures.Now).ShouldFailWith("ticket.not_owned_by_holder");
    }
}
