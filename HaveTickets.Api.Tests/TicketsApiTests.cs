using System.Net;
using System.Net.Http.Json;
using HaveTickets.Contracts.V1;
using Shouldly;

namespace HaveTickets.Api.Tests;

public sealed class TicketsApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public TicketsApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<IReadOnlyList<TicketResponse>> TicketsOfAsync(Guid holder) =>
        await _client.GetFromJsonAsync<IReadOnlyList<TicketResponse>>($"/api/v1/tickets?holderId={holder}")
        ?? throw new InvalidOperationException();

    [Fact]
    public async Task A_holder_sees_only_their_own_tickets()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "A1", "A2");
        await _client.BuyOkAsync(showtime.Id, ApiHelpers.OtherHolder, "B1");

        (await TicketsOfAsync(ApiHelpers.Holder)).Select(ticket => ticket.Seat).Order().ShouldBe(["A1", "A2"]);
        (await TicketsOfAsync(ApiHelpers.OtherHolder)).Select(ticket => ticket.Seat).ShouldBe(["B1"]);
        (await TicketsOfAsync(Guid.NewGuid())).ShouldBeEmpty();
    }

    [Fact]
    public async Task Listing_tickets_without_a_holder_is_rejected()
    {
        HttpResponseMessage missing = await _client.GetAsync("/api/v1/tickets");
        HttpResponseMessage empty = await _client.GetAsync($"/api/v1/tickets?holderId={Guid.Empty}");

        missing.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_qr_endpoint_renders_a_png_for_a_known_ticket()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "A1");

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/tickets/{order.Tickets[0].Id}/qr");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(8).ShouldBe([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    }

    [Fact]
    public async Task The_qr_endpoint_answers_not_found_for_an_unknown_ticket()
    {
        (await _client.GetAsync($"/api/v1/tickets/{Guid.NewGuid()}/qr")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancelling_a_ticket_puts_its_seat_back_on_sale()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "G4");

        HttpResponseMessage cancel = await _client.DeleteAsync(
            $"/api/v1/tickets/{order.Tickets[0].Id}?holderId={ApiHelpers.Holder}");

        cancel.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.BuyAsync(showtime.Id, ApiHelpers.OtherHolder, "G4")).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cancelling_somebody_elses_ticket_is_forbidden_and_changes_nothing()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "G5");

        HttpResponseMessage cancel = await _client.DeleteAsync(
            $"/api/v1/tickets/{order.Tickets[0].Id}?holderId={ApiHelpers.OtherHolder}");

        cancel.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await cancel.ReadProblemAsync()).ErrorCode.ShouldBe("ticket.not_owned_by_holder");
        (await TicketsOfAsync(ApiHelpers.Holder)).Single().Status.ShouldBe("Issued");
    }

    [Fact]
    public async Task Cancelling_after_sales_close_is_a_conflict()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "G6");

        _factory.Clock.SetUtcNow(showtime.StartsAt - TimeSpan.FromMinutes(10));
        HttpResponseMessage cancel = await _client.DeleteAsync(
            $"/api/v1/tickets/{order.Tickets[0].Id}?holderId={ApiHelpers.Holder}");

        cancel.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await cancel.ReadProblemAsync()).ErrorCode.ShouldBe("ticket.too_late_to_cancel");
    }

    [Fact]
    public async Task Cancelling_an_unknown_ticket_is_not_found()
    {
        HttpResponseMessage cancel = await _client.DeleteAsync(
            $"/api/v1/tickets/{Guid.NewGuid()}?holderId={ApiHelpers.Holder}");

        cancel.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Check_in_is_a_scan_outcome_so_a_refusal_still_answers_200()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "H1");
        string code = order.Tickets[0].Code;

        HttpResponseMessage tooEarly = await _client.CheckInAsync(code);

        tooEarly.StatusCode.ShouldBe(HttpStatusCode.OK);
        CheckInResponse refusal = await tooEarly.ReadAsync<CheckInResponse>();
        refusal.Admitted.ShouldBeFalse();
        refusal.Outcome.ShouldBe("ticket.check_in_not_open_yet");
    }

    [Fact]
    public async Task A_ticket_is_admitted_once_inside_the_window_and_never_twice()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "H2");
        string code = order.Tickets[0].Code;

        _factory.Clock.SetUtcNow(showtime.StartsAt - TimeSpan.FromMinutes(30));
        CheckInResponse first = await (await _client.CheckInAsync(code)).ReadAsync<CheckInResponse>();
        CheckInResponse second = await (await _client.CheckInAsync(code)).ReadAsync<CheckInResponse>();

        first.Admitted.ShouldBeTrue();
        first.Ticket!.Status.ShouldBe("CheckedIn");
        second.Admitted.ShouldBeFalse();
        second.Outcome.ShouldBe("ticket.already_checked_in");
    }

    [Fact]
    public async Task Check_in_is_refused_after_the_grace_period()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "H3");

        _factory.Clock.SetUtcNow(showtime.StartsAt + TimeSpan.FromMinutes(31));
        CheckInResponse result = await (await _client.CheckInAsync(order.Tickets[0].Code)).ReadAsync<CheckInResponse>();

        result.Admitted.ShouldBeFalse();
        result.Outcome.ShouldBe("ticket.check_in_closed");
    }

    [Fact]
    public async Task Check_in_refuses_cancelled_unknown_and_malformed_codes_without_failing_the_scan()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();
        OrderResponse order = await _client.BuyOkAsync(showtime.Id, ApiHelpers.Holder, "H4");
        await _client.DeleteAsync($"/api/v1/tickets/{order.Tickets[0].Id}?holderId={ApiHelpers.Holder}");
        _factory.Clock.SetUtcNow(showtime.StartsAt - TimeSpan.FromMinutes(30));

        CheckInResponse cancelled = await (await _client.CheckInAsync(order.Tickets[0].Code)).ReadAsync<CheckInResponse>();
        CheckInResponse unknown = await (await _client.CheckInAsync("ZZZZZZZZ")).ReadAsync<CheckInResponse>();
        CheckInResponse malformed = await (await _client.CheckInAsync("nope")).ReadAsync<CheckInResponse>();

        cancelled.Outcome.ShouldBe("ticket.cancelled");
        unknown.Outcome.ShouldBe("ticket.not_found_by_code");
        malformed.Outcome.ShouldBe("confirmation_code.invalid_format");
        new[] { cancelled, unknown, malformed }.ShouldAllBe(result => !result.Admitted);
    }
}
