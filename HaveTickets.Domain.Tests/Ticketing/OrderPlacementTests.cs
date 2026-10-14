using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;
using HaveTickets.Domain.Venues;
using Shouldly;

namespace HaveTickets.Domain.Tests.Ticketing;

public sealed class OrderPlacementTests
{
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private readonly Auditorium _auditorium = Fixtures.CreateAuditorium();

    private Showtime Showtime(TimeSpan startsIn, decimal price = 9.50m) =>
        Fixtures.CreateShowtime(_auditorium, startsIn, price);

    private Result<Order> Place(
        Showtime showtime,
        IReadOnlyList<SeatNumber> seats,
        IReadOnlySet<string>? occupied = null,
        Guid? holder = null,
        DateTimeOffset? now = null) =>
        Order.Place(
            showtime,
            _auditorium,
            holder ?? Fixtures.Holder,
            seats,
            occupied ?? Fixtures.Occupied(),
            now ?? Fixtures.Now);

    [Fact]
    public void Place_with_valid_seats_issues_one_ticket_per_seat()
    {
        Result<Order> result = Place(Showtime(OneDay), Fixtures.Seats("A1", "A2", "B7"));

        result.ShouldSucceed();
        result.Value.Tickets.Count.ShouldBe(3);
        result.Value.Tickets.ShouldAllBe(ticket => ticket.HolderId == Fixtures.Holder);
        result.Value.Tickets.ShouldAllBe(ticket => ticket.Status == TicketStatus.Issued);
    }

    [Fact]
    public void Place_totals_the_price_of_every_seat_exactly()
    {
        Result<Order> result = Place(Showtime(OneDay, price: 8.30m), Fixtures.Seats("A1", "A2", "A3"));

        result.ShouldSucceed();
        result.Value.Total.AmountInMinorUnits.ShouldBe(2490);
    }

    [Fact]
    public void Place_snapshots_the_price_on_each_ticket()
    {
        Result<Order> result = Place(Showtime(OneDay, price: 12.00m), Fixtures.Seats("C3"));

        result.ShouldSucceed();
        result.Value.Tickets.Single().PricePaid.AmountInMinorUnits.ShouldBe(1200);
    }

    [Fact]
    public void Place_issues_tickets_in_seat_order_regardless_of_request_order()
    {
        Result<Order> result = Place(Showtime(OneDay), Fixtures.Seats("C2", "A9", "B1"));

        result.ShouldSucceed();
        result.Value.Tickets.Select(ticket => ticket.Seat.Label).ShouldBe(["A9", "B1", "C2"]);
    }

    [Fact]
    public void Place_gives_every_ticket_its_own_code()
    {
        Result<Order> result = Place(Showtime(OneDay), Fixtures.Seats("A1", "A2", "A3", "A4", "A5", "A6"));

        result.ShouldSucceed();
        result.Value.Tickets.Select(ticket => ticket.Code.Value).Distinct().Count().ShouldBe(6);
    }

    [Fact]
    public void Place_accepts_exactly_the_per_order_limit()
    {
        string[] labels = [.. Enumerable.Range(1, Order.MaxSeatsPerOrder).Select(number => $"A{number}")];

        Place(Showtime(OneDay), Fixtures.Seats(labels)).ShouldSucceed();
    }

    [Fact]
    public void Place_rejects_one_seat_over_the_per_order_limit()
    {
        string[] labels = [.. Enumerable.Range(1, Order.MaxSeatsPerOrder + 1).Select(number => $"A{number}")];

        Place(Showtime(OneDay), Fixtures.Seats(labels)).ShouldFailWith("order.too_many_seats");
    }

    [Fact]
    public void Place_rejects_an_empty_selection()
    {
        Place(Showtime(OneDay), []).ShouldFailWith("order.no_seats_selected");
    }

    [Fact]
    public void Place_rejects_a_missing_holder()
    {
        Place(Showtime(OneDay), Fixtures.Seats("A1"), holder: Guid.Empty).ShouldFailWith("order.holder_required");
    }

    [Fact]
    public void Place_rejects_the_same_seat_twice_in_one_order()
    {
        Place(Showtime(OneDay), Fixtures.Seats("A1", "A1")).ShouldFailWith("seat.duplicated");
    }

    [Fact]
    public void Place_rejects_a_seat_outside_the_auditorium()
    {
        // The default auditorium has 10 rows of 12 seats.
        Place(Showtime(OneDay), Fixtures.Seats("K1")).ShouldFailWith("seat.not_in_auditorium");
        Place(Showtime(OneDay), Fixtures.Seats("A13")).ShouldFailWith("seat.not_in_auditorium");
    }

    [Fact]
    public void Place_reports_a_taken_seat_as_a_conflict()
    {
        Result<Order> result = Place(
            Showtime(OneDay),
            Fixtures.Seats("A1", "A2"),
            occupied: Fixtures.Occupied("A2"));

        result.ShouldFailWith("seat.already_taken");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void Place_is_all_or_nothing_when_a_single_seat_is_taken()
    {
        Result<Order> result = Place(
            Showtime(OneDay),
            Fixtures.Seats("A1", "A2", "A3"),
            occupied: Fixtures.Occupied("A3"));

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Place_stays_open_until_the_sales_cut_off()
    {
        Showtime showtime = Showtime(OneDay);

        Place(showtime, Fixtures.Seats("A1"), now: showtime.SalesCloseAt - TimeSpan.FromSeconds(1)).ShouldSucceed();
    }

    [Fact]
    public void Place_closes_exactly_at_the_sales_cut_off()
    {
        Showtime showtime = Showtime(OneDay);

        Place(showtime, Fixtures.Seats("A1"), now: showtime.SalesCloseAt).ShouldFailWith("showtime.sales_closed");
    }

    [Fact]
    public void Place_rejects_a_showtime_that_already_started()
    {
        Place(Showtime(-TimeSpan.FromMinutes(5)), Fixtures.Seats("A1")).ShouldFailWith("showtime.sales_closed");
    }

    [Fact]
    public void Place_rejects_an_auditorium_that_does_not_host_the_showtime()
    {
        Auditorium other = Fixtures.CreateAuditorium();

        Result<Order> result = Order.Place(
            Showtime(OneDay),
            other,
            Fixtures.Holder,
            Fixtures.Seats("A1"),
            Fixtures.Occupied(),
            Fixtures.Now);

        result.ShouldFailWith("order.auditorium_mismatch");
    }

    [Fact]
    public void Place_reports_validation_before_availability()
    {
        // A seat outside the room is a client mistake even if it is also listed as occupied.
        Place(Showtime(OneDay), Fixtures.Seats("Z99"), occupied: Fixtures.Occupied("Z99"))
            .ShouldFailWith("seat.not_in_auditorium");
    }
}
