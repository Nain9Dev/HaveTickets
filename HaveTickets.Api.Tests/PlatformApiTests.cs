using System.Net;
using System.Net.Http.Json;
using HaveTickets.Contracts.V1;
using Shouldly;

namespace HaveTickets.Api.Tests;

public sealed class PlatformApiTests
{
    [Fact]
    public async Task Liveness_and_readiness_probes_answer_healthy()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateClient();

        (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_openapi_document_describes_the_v1_surface()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string document = await response.Content.ReadAsStringAsync();
        document.ShouldContain("/api/v1/orders");
        document.ShouldContain("/api/v1/tickets/check-in");
        document.ShouldContain("/api/v1/showtimes/{showtimeId}/seat-map");
    }

    [Fact]
    public async Task A_configured_origin_is_allowed_to_call_the_api()
    {
        using var factory = new ApiFactory("Production");
        using HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/genres");
        request.Headers.Add("Origin", "https://nain9dev.github.io");

        HttpResponseMessage response = await client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["https://nain9dev.github.io"]);
    }

    [Fact]
    public async Task An_unlisted_origin_gets_no_cors_grant()
    {
        using var factory = new ApiFactory("Production");
        using HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/genres");
        request.Headers.Add("Origin", "https://evil.example");

        HttpResponseMessage response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task A_preflight_from_an_unlisted_origin_is_not_approved()
    {
        using var factory = new ApiFactory("Production");
        using HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/orders");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Writes_are_throttled_per_client_with_a_retry_hint()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateClient();
        var empty = new PurchaseTicketsRequest(Guid.NewGuid(), Guid.Empty, []);

        HttpStatusCode last = HttpStatusCode.OK;
        HttpResponseMessage? throttled = null;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/orders", empty);
            last = response.StatusCode;
            if (last == HttpStatusCode.TooManyRequests)
            {
                throttled = response;
                break;
            }
        }

        last.ShouldBe(HttpStatusCode.TooManyRequests);
        throttled.ShouldNotBeNull();
        throttled.Headers.Contains("Retry-After").ShouldBeTrue();
        (await throttled.Content.ReadAsStringAsync()).ShouldContain("rate_limit.exceeded");
    }

    [Fact]
    public async Task Reads_have_their_own_larger_budget_than_writes()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateClient();

        for (int attempt = 0; attempt < 40; attempt++)
        {
            (await client.GetAsync("/api/v1/genres")).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
