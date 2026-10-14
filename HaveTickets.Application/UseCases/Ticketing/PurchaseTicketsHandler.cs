using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;

namespace HaveTickets.Application.UseCases.Ticketing;

public sealed record PurchaseTicketsCommand(Guid ShowtimeId, Guid HolderId, IReadOnlyList<string> Seats);

/// <summary>
/// Buys one or more seats for a showtime.
/// <para>
/// Correctness rests on two layers, not one. The aggregate rejects everything it
/// can see from a recent read, which produces precise messages; the unique index
/// on (ShowtimeId, Seat) rejects what only the database can see, which is the
/// race between two buyers clicking the same seat in the same millisecond. Under
/// contention exactly one order survives and the loser gets
/// <c>seat.already_taken</c>, never a half-written order.
/// </para>
/// </summary>
public sealed class PurchaseTicketsHandler(
    IShowtimeRepository showtimes,
    IOrderRepository orders,
    ITicketReadModel ticketReadModel,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<Result<OrderResponse>> HandleAsync(
        PurchaseTicketsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<List<SeatNumber>> seats = ParseSeats(command.Seats);
        if (seats.IsFailure)
        {
            return seats.Error;
        }

        ShowtimeWithVenue? venue = await showtimes.GetWithVenueAsync(command.ShowtimeId, cancellationToken);
        if (venue is null)
        {
            return DomainErrors.Showtime.NotFound(command.ShowtimeId);
        }

        IReadOnlySet<string> occupied = await showtimes.GetOccupiedSeatLabelsAsync(
            command.ShowtimeId,
            cancellationToken);

        Result<Order> order = Order.Place(
            venue.Showtime,
            venue.Auditorium,
            command.HolderId,
            seats.Value,
            occupied,
            clock.GetUtcNow());

        if (order.IsFailure)
        {
            return order.Error;
        }

        orders.Add(order.Value);

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        OrderResponse? response = await ticketReadModel.GetOrderAsync(order.Value.Id, cancellationToken);
        return response is null
            ? DomainErrors.Order.NotFound(order.Value.Id)
            : response;
    }

    private static Result<List<SeatNumber>> ParseSeats(IReadOnlyList<string>? labels)
    {
        if (labels is null || labels.Count == 0)
        {
            return DomainErrors.Order.NoSeatsSelected;
        }

        var seats = new List<SeatNumber>(labels.Count);
        foreach (string label in labels)
        {
            Result<SeatNumber> seat = SeatNumber.Parse(label);
            if (seat.IsFailure)
            {
                return seat.Error;
            }

            seats.Add(seat.Value);
        }

        return seats;
    }
}
