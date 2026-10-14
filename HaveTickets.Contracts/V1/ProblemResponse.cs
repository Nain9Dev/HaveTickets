namespace HaveTickets.Contracts.V1;

/// <summary>
/// RFC 9457 problem details as returned by this API, with the domain error code
/// carried in an extension member so clients can branch on it.
/// </summary>
public sealed record ProblemResponse
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public int? Status { get; init; }

    public string? Detail { get; init; }

    /// <summary>Stable domain error code, for example <c>seat.already_taken</c>.</summary>
    public string? ErrorCode { get; init; }

    public string? TraceId { get; init; }
}
