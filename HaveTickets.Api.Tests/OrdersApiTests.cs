using System.Net;
using System.Net.Http.Json;
using HaveTickets.Contracts.V1;
using Shouldly;

namespace HaveTickets.Api.Tests;

public sealed class OrdersApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public OrdersApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Buying_seats_creates_an_order_with_one_ticket_per_seat()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();

        HttpResponseMessage response = await _client.BuyAsync(showtime.Id, ApiHelpers.Holder, "A1", "A2");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        OrderResponse order = await response.ReadAsync<OrderResponse>();
        order.Tickets.Select(ticket => ticket.Seat).ShouldBe(["A1", "A2"]);
        order.Total.ShouldBe(showtime.Price * 2);
        order.Currency.ShouldBe(showtime.Currency);
        order.Reference.Length.ShouldBe(8);
    }

    [Fact]
    public async Task A_sold_seat_shows_up_as_occupied_in_the_seat_map()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "B3");

        SeatMapResponse map = await _client.GetFromJsonAsync<SeatMapResponse>(
            $"/api/v1/showtimes/{showtime.Id}/seat-map") ?? throw new InvalidOperationException();

        map.OccupiedSeats.ShouldBe(["B3"]);
        map.SalesOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task Selling_the_same_seat_twice_is_a_conflict_with_a_stable_error_code()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "C5");

        HttpResponseMessage second = await _client.BuyAsync(showtime.Id, ApiHelpers.OtherHolder, "C5");

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ProblemResponse problem = await second.ReadProblemAsync();
        problem.ErrorCode.ShouldBe("seat.already_taken");
        problem.Status.ShouldBe(409);
        problem.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_conflicting_order_leaves_none_of_its_other_seats_sold()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "D2");

        HttpResponseMessage rejected = await _client.BuyAsync(showtime.Id, ApiHelpers.OtherHolder, "D1", "D2", "D3");

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        SeatMapResponse map = await _client.GetFromJsonAsync<SeatMapResponse>(
            $"/api/v1/showtimes/{showtime.Id}/seat-map") ?? throw new InvalidOperationException();
        map.OccupiedSeats.ShouldBe(["D2"]);
    }

    [Fact]
    public async Task Concurrent_buyers_of_one_seat_produce_exactly_one_winner()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        Guid[] buyers = [.. Enumerable.Range(0, 12).Select(_ => Guid.NewGuid())];

        HttpResponseMessage[] responses = await Task.WhenAll(
            buyers.Select(buyer => _client.BuyAsync(showtime.Id, buyer, "E7")));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(buyers.Length - 1);

        SeatMapResponse map = await _client.GetFromJsonAsync<SeatMapResponse>(
            $"/api/v1/showtimes/{showtime.Id}/seat-map") ?? throw new InvalidOperationException();
        map.OccupiedSeats.ShouldBe(["E7"]);
    }

    [Fact]
    public async Task Concurrent_buyers_of_different_seats_all_succeed()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        string[] seats = [.. Enumerable.Range(1, 10).Select(number => $"F{number}")];

        HttpResponseMessage[] responses = await Task.WhenAll(
            seats.Select(seat => _client.BuyAsync(showtime.Id, Guid.NewGuid(), seat)));

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("order.no_seats_selected")]
    [InlineData("seat.invalid_label")]
    [InlineData("seat.not_in_auditorium")]
    [InlineData("order.too_many_seats")]
    [InlineData("order.holder_required")]
    public async Task Invalid_orders_are_rejected_with_a_validation_problem(string expectedCode)
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        Guid holder = ApiHelpers.Holder;
        string[] seats = ["A1"];

        switch (expectedCode)
        {
            case "order.no_seats_selected":
                seats = [];
                break;
            case "seat.invalid_label":
                seats = ["7C"];
                break;
            case "seat.not_in_auditorium":
                seats = ["Z99"];
                break;
            case "order.too_many_seats":
                seats = ["A1", "A2", "A3", "A4", "A5", "A6", "A7"];
                break;
            case "order.holder_required":
                holder = Guid.Empty;
                break;
        }

        HttpResponseMessage response = await _client.BuyAsync(showtime.Id, holder, seats);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).ErrorCode.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task Buying_for_an_unknown_showtime_is_not_found()
    {
        HttpResponseMessage response = await _client.BuyAsync(Guid.NewGuid(), ApiHelpers.Holder, "A1");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadProblemAsync()).ErrorCode.ShouldBe("showtime.not_found");
    }

    [Fact]
    public async Task Sales_close_fifteen_minutes_before_the_showtime()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();

        _factory.Clock.SetUtcNow(showtime.StartsAt - TimeSpan.FromMinutes(15));
        HttpResponseMessage response = await _client.BuyAsync(showtime.Id, ApiHelpers.Holder, "A1");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).ErrorCode.ShouldBe("showtime.sales_closed");
    }

    [Fact]
    public async Task Sales_are_still_open_one_second_before_the_cut_off()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();

        _factory.Clock.SetUtcNow(showtime.StartsAt - TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));

        (await _client.BuyAsync(showtime.Id, ApiHelpers.Holder, "A1")).StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
