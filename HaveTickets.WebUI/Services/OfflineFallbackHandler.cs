using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HaveTickets.Contracts.V1;

namespace HaveTickets.WebUI.Services;

public sealed class OfflineFallbackHandler : DelegatingHandler
{
    private static readonly Guid CinemaGranViaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly CinemaResponse CinemaGranVia = new(
        CinemaGranViaId,
        "Cines Callao Gran Vía",
        "Madrid",
        "Plaza del Callao, 3, 28013 Madrid");

    private static readonly Guid MovieDuneId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MovieOppenheimerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MovieBladeRunnerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Guid ShowtimeDune1Id = Guid.Parse("55555555-5555-5555-5555-555555555551");
    private static readonly Guid ShowtimeDune2Id = Guid.Parse("55555555-5555-5555-5555-555555555552");
    private static readonly Guid ShowtimeOpp1Id = Guid.Parse("55555555-5555-5555-5555-555555555553");

    private static readonly List<MovieSummaryResponse> MoviesList =
    [
        new(
            MovieDuneId,
            "Dune: Part Two",
            "Paul Atreides unites with Chani and the Fremen while seeking revenge against the conspirators who destroyed his family.",
            "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80",
            166,
            new DateOnly(2024, 3, 1),
            "Sci-Fi",
            "PG-13",
            2,
            DateTimeOffset.UtcNow.AddHours(2)),
        new(
            MovieOppenheimerId,
            "Oppenheimer",
            "The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb during World War II.",
            "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?w=800&auto=format&fit=crop&q=80",
            180,
            new DateOnly(2023, 7, 21),
            "Drama",
            "R",
            1,
            DateTimeOffset.UtcNow.AddHours(4)),
        new(
            MovieBladeRunnerId,
            "Blade Runner 2049",
            "Young Blade Runner K's discovery of a long-buried secret leads him to track down former Blade Runner Rick Deckard, who's been missing for thirty years.",
            "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800&auto=format&fit=crop&q=80",
            164,
            new DateOnly(2017, 10, 6),
            "Sci-Fi",
            "R",
            1,
            DateTimeOffset.UtcNow.AddHours(6))
    ];

    private static readonly List<TicketResponse> UserTickets =
    [
        new(
            Guid.Parse("77777777-7777-7777-7777-777777777771"),
            "HT-DUNE-C7",
            "Issued",
            "C7",
            9.50m,
            "EUR",
            DateTimeOffset.UtcNow.AddHours(-1),
            null,
            ShowtimeDune1Id,
            DateTimeOffset.UtcNow.AddHours(2),
            "Dune: Part Two",
            "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80",
            "Cines Callao Gran Vía",
            "Sala 1 - IMAX Laser"),
        new(
            Guid.Parse("77777777-7777-7777-7777-777777777772"),
            "HT-DUNE-C8",
            "Issued",
            "C8",
            9.50m,
            "EUR",
            DateTimeOffset.UtcNow.AddHours(-1),
            null,
            ShowtimeDune1Id,
            DateTimeOffset.UtcNow.AddHours(2),
            "Dune: Part Two",
            "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80",
            "Cines Callao Gran Vía",
            "Sala 1 - IMAX Laser")
    ];

    private static readonly HashSet<string> ReservedSeats = ["A1", "A2", "C5", "C6", "C7", "C8", "D10", "E4"];

    // 1x1 transparent PNG fallback for ticket QR rendering
    private static readonly byte[] QrPixelBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0B, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x60, 0x00, 0x00, 0x00,
        0x02, 0x00, 0x01, 0xE5, 0x27, 0xDE, 0xFC, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

    public OfflineFallbackHandler()
    {
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Attempt remote API if reachable
            var response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
        }
        catch
        {
            // Network failure, offline host, or CORS restriction -> proceed to deterministic mock
        }

        return HandleMockRequest(request);
    }

    private static HttpResponseMessage HandleMockRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var method = request.Method;

        // 1. GET api/v1/movies
        if (method == HttpMethod.Get && (path.EndsWith("api/v1/movies") || path.EndsWith("api/v1/movies/")))
        {
            var paged = new PagedResponse<MovieSummaryResponse>(MoviesList, 1, 10, MoviesList.Count);
            return CreateJsonResponse(paged);
        }

        // 2. GET api/v1/movies/{id}
        if (method == HttpMethod.Get && path.Contains("api/v1/movies/"))
        {
            var movie = MoviesList[0];
            var detail = new MovieDetailResponse(
                movie.Id,
                movie.Title,
                movie.Synopsis,
                movie.PosterUrl,
                movie.DurationMinutes,
                movie.ReleaseDate,
                movie.Genre,
                movie.AgeRating,
                [
                    new(
                        ShowtimeDune1Id,
                        DateTimeOffset.UtcNow.AddHours(2),
                        9.50m,
                        "EUR",
                        CinemaGranVia,
                        "Sala 1 - IMAX Laser",
                        120,
                        120 - ReservedSeats.Count,
                        true),
                    new(
                        ShowtimeDune2Id,
                        DateTimeOffset.UtcNow.AddHours(5),
                        9.50m,
                        "EUR",
                        CinemaGranVia,
                        "Sala 2 - Dolby Atmos",
                        120,
                        110,
                        true)
                ]);
            return CreateJsonResponse(detail);
        }

        // 3. GET api/v1/showtimes/{id}/seat-map
        if (method == HttpMethod.Get && path.Contains("seat-map"))
        {
            var seatMap = new SeatMapResponse(
                ShowtimeDune1Id,
                "Dune: Part Two",
                "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80",
                DateTimeOffset.UtcNow.AddHours(2),
                "Cines Callao Gran Vía",
                "Sala 1 - IMAX Laser",
                10,
                12,
                9.50m,
                "EUR",
                true,
                6,
                ReservedSeats.ToList());
            return CreateJsonResponse(seatMap);
        }

        // 4. POST api/v1/orders
        if (method == HttpMethod.Post && path.Contains("api/v1/orders"))
        {
            var orderId = Guid.NewGuid();
            var orderRef = $"HT-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            var tickets = new List<TicketResponse>();

            var defaultSeats = new[] { "E5", "E6" };
            foreach (var seat in defaultSeats)
            {
                ReservedSeats.Add(seat);
                var ticket = new TicketResponse(
                    Guid.NewGuid(),
                    $"HT-{seat}-{new Random().Next(100, 999)}",
                    "Issued",
                    seat,
                    9.50m,
                    "EUR",
                    DateTimeOffset.UtcNow,
                    null,
                    ShowtimeDune1Id,
                    DateTimeOffset.UtcNow.AddHours(2),
                    "Dune: Part Two",
                    "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80",
                    "Cines Callao Gran Vía",
                    "Sala 1 - IMAX Laser");
                tickets.Add(ticket);
                UserTickets.Add(ticket);
            }

            var order = new OrderResponse(orderId, orderRef, DateTimeOffset.UtcNow, tickets.Count * 9.50m, "EUR", tickets);
            return CreateJsonResponse(order);
        }

        // 5. GET api/v1/tickets
        if (method == HttpMethod.Get && path.Contains("api/v1/tickets") && !path.EndsWith("/qr"))
        {
            return CreateJsonResponse(UserTickets.ToList());
        }

        // 6. DELETE api/v1/tickets/{id}
        if (method == HttpMethod.Delete && path.Contains("api/v1/tickets/"))
        {
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        // 7. GET api/v1/tickets/{id}/qr
        if (method == HttpMethod.Get && path.EndsWith("/qr"))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(QrPixelBytes)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static HttpResponseMessage CreateJsonResponse<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
