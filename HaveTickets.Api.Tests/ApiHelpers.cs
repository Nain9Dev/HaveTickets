using System.Net;
using System.Net.Http.Json;
using HaveTickets.Contracts.V1;
using Shouldly;

namespace HaveTickets.Api.Tests;

internal static class ApiHelpers
{
    public static readonly Guid Holder = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid OtherHolder = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        where T : class
    {
        T? value = await response.Content.ReadFromJsonAsync<T>();
        value.ShouldNotBeNull();
        return value;
    }

    public static async Task<ProblemResponse> ReadProblemAsync(this HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        return await response.ReadAsync<ProblemResponse>();
    }

    /// <summary>Returns the first showtime of the first movie that is on sale.</summary>
    public static async Task<ShowtimeResponse> FirstOpenShowtimeAsync(this HttpClient client)
    {
        HttpResponseMessage listing = await client.GetAsync("/api/v1/movies?pageSize=48");
        listing.StatusCode.ShouldBe(HttpStatusCode.OK);
        PagedResponse<MovieSummaryResponse> page = await listing.ReadAsync<PagedResponse<MovieSummaryResponse>>();

        MovieSummaryResponse movie = page.Items.First(candidate => candidate.ShowtimeCount > 0);
        MovieDetailResponse detail = await client.GetFromJsonAsync<MovieDetailResponse>($"/api/v1/movies/{movie.Id}")
            ?? throw new InvalidOperationException("Movie detail was empty.");

        return detail.Showtimes.OrderBy(showtime => showtime.StartsAt).First(showtime => showtime.SalesOpen);
    }

    public static Task<HttpResponseMessage> BuyAsync(
        this HttpClient client,
        Guid showtimeId,
        Guid holderId,
        params string[] seats) =>
        client.PostAsJsonAsync("/api/v1/orders", new PurchaseTicketsRequest(showtimeId, holderId, seats));

    public static async Task<OrderResponse> BuyOkAsync(
        this HttpClient client,
        Guid showtimeId,
        Guid holderId,
        params string[] seats)
    {
        HttpResponseMessage response = await client.BuyAsync(showtimeId, holderId, seats);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await response.ReadAsync<OrderResponse>();
    }

    public static Task<HttpResponseMessage> CheckInAsync(this HttpClient client, string code) =>
        client.PostAsJsonAsync("/api/v1/tickets/check-in", new CheckInRequest(code));
}
