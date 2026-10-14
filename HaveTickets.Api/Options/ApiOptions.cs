namespace HaveTickets.Api.Options;

/// <summary>Cross-origin policy, bound from the <c>Cors</c> configuration section.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Origins allowed to call the API. Empty means "same origin only": the API is
    /// never opened to the whole internet by omission.
    /// </summary>
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];
}

/// <summary>Request quotas, bound from the <c>RateLimiting</c> configuration section.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Reads and other safe requests, per client address per window.</summary>
    public int ReadPermitsPerWindow { get; init; } = 120;

    /// <summary>Purchases and other writes, per client address per window.</summary>
    public int WritePermitsPerWindow { get; init; } = 20;

    public int WindowSeconds { get; init; } = 60;
}
