namespace HaveTickets.Domain.Ticketing;

/// <summary>
/// Lifecycle of a ticket. Transitions are one-way:
/// Issued -> CheckedIn, or Issued -> Cancelled.
/// </summary>
public enum TicketStatus
{
    /// <summary>Sold and valid.</summary>
    Issued = 0,

    /// <summary>Redeemed at the door. Cannot be used again.</summary>
    CheckedIn = 1,

    /// <summary>Voided before the sales window closed. The seat is released.</summary>
    Cancelled = 2
}
