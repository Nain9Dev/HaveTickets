using System.Net;
using System.Net.Http.Json;
using HaveTickets.Contracts.V1;
using Shouldly;

namespace HaveTickets.Api.Tests;

public sealed class CatalogApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public CatalogApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<PagedResponse<MovieSummaryResponse>> ListAsync(string query = "") =>
        await _client.GetFromJsonAsync<PagedResponse<MovieSummaryResponse>>($"/api/v1/movies{query}")
        ?? throw new InvalidOperationException();

    [Fact]
    public async Task The_seeded_catalogue_is_paged()
    {
        PagedResponse<MovieSummaryResponse> first = await ListAsync("?page=1&pageSize=5");
        PagedResponse<MovieSummaryResponse> second = await ListAsync("?page=2&pageSize=5");

        first.Items.Count.ShouldBe(5);
        first.TotalCount.ShouldBeGreaterThan(5);
        first.HasNext.ShouldBeTrue();
        first.HasPrevious.ShouldBeFalse();
        second.HasPrevious.ShouldBeTrue();
        first.Items.Select(movie => movie.Id).Intersect(second.Items.Select(movie => movie.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_page_size_is_capped_instead_of_trusted()
    {
        PagedResponse<MovieSummaryResponse> page = await ListAsync("?pageSize=100000");

        page.PageSize.ShouldBeLessThanOrEqualTo(48);
        page.Items.Count.ShouldBeLessThanOrEqualTo(48);
    }

    [Fact]
    public async Task Searching_by_title_narrows_the_listing()
    {
        MovieSummaryResponse sample = (await ListAsync("?pageSize=1")).Items.Single();
        string fragment = sample.Title[..Math.Min(4, sample.Title.Length)];

        PagedResponse<MovieSummaryResponse> result = await ListAsync($"?search={Uri.EscapeDataString(fragment)}");

        result.Items.ShouldContain(movie => movie.Id == sample.Id);
        result.Items.ShouldAllBe(movie => movie.Title.Contains(fragment, StringComparison.OrdinalIgnoreCase)
            || movie.Synopsis.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Filtering_by_genre_returns_only_that_genre()
    {
        string genre = (await ListAsync("?pageSize=1")).Items.Single().Genre;

        PagedResponse<MovieSummaryResponse> result = await ListAsync($"?genre={Uri.EscapeDataString(genre)}");

        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldAllBe(movie => movie.Genre == genre);
    }

    [Fact]
    public async Task The_genre_list_has_no_duplicates()
    {
        IReadOnlyList<string> genres = await _client.GetFromJsonAsync<IReadOnlyList<string>>("/api/v1/genres")
            ?? throw new InvalidOperationException();

        genres.ShouldNotBeEmpty();
        genres.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(genres.Count);
    }

    [Fact]
    public async Task A_movie_detail_lists_its_upcoming_showtimes_with_availability()
    {
        ShowtimeResponse showtime = await _client.FirstOpenShowtimeAsync();

        showtime.Capacity.ShouldBeGreaterThan(0);
        showtime.SeatsAvailable.ShouldBe(showtime.Capacity);
        showtime.SalesOpen.ShouldBeTrue();
        showtime.Cinema.Name.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_unknown_movie_is_a_problem_with_a_stable_error_code()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/movies/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadProblemAsync()).ErrorCode.ShouldBe("movie.not_found");
    }

    [Fact]
    public async Task An_unknown_seat_map_is_not_found()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/showtimes/{Guid.NewGuid()}/seat-map");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_the_catalogue()
    {
        int before = (await ListAsync("?pageSize=1")).TotalCount;

        using var second = new ApiFactory();
        using HttpClient other = second.CreateClient();
        PagedResponse<MovieSummaryResponse> again = await other.GetFromJsonAsync<PagedResponse<MovieSummaryResponse>>(
            "/api/v1/movies?pageSize=1") ?? throw new InvalidOperationException();

        again.TotalCount.ShouldBe(before);
    }
}
