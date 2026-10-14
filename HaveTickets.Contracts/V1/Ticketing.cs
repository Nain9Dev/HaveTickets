namespace HaveTickets.Contracts.V1;

/// <summary>Seats are sent as canonical labels, for example ["C7", "C8"].</summary>
public sealed record PurchaseTicketsRequest(
    Guid ShowtimeId,
    Guid HolderId,
    IReadOnlyList<string> Seats);

public sealed record OrderResponse(
    Guid Id,
    string Reference,
    DateTimeOffset PlacedAt,
    decimal Total,
    string Currency,
    IReadOnlyList<TicketResponse> Tickets);

public sealed record TicketResponse(
    Guid Id,
    string Code,
    string Status,
    string Seat,
    decimal PricePaid,
    string Currency,
    DateTimeOffset IssuedAt,
    DateTimeOffset? CheckedInAt,
    Guid ShowtimeId,
    DateTimeOffset StartsAt,
    string MovieTitle,
    string PosterUrl,
    string CinemaName,
    string AuditoriumName);

public sealed record CheckInRequest(string Code);

/// <summary>
/// The door scanner answer. <paramref name="Admitted"/> is the only field a
/// turnstile needs; the rest is for the operator looking at the screen.
/// </summary>
public sealed record CheckInResponse(
    bool Admitted,
    string Outcome,
    string Message,
    TicketResponse? Ticket);
