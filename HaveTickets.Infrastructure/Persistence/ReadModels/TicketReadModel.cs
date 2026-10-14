using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Ticketing queries. Filters are applied to the entity before projection so the
/// value-converted columns (Seat, Code) are compared in SQL, not in memory.
/// </summary>
public sealed class TicketReadModel(HaveTicketsDbContext database) : ITicketReadModel
{
    private const decimal MinorUnitsPerUnit = 100m;

    public async Task<IReadOnlyList<TicketResponse>> ListByHolderAsync(
        Guid holderId,
        CancellationToken cancellationToken = default)
    {
        List<TicketRow> rows = await Project(
                database.Tickets.AsNoTracking()
                    .Where(ticket => ticket.HolderId == holderId)
                    .OrderByDescending(ticket => ticket.Showtime!.StartsAt))
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(ToResponse);
    }

    public async Task<TicketResponse?> GetTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        TicketRow? row = await Project(
                database.Tickets.AsNoTracking().Where(ticket => ticket.Id == ticketId))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToResponse(row);
    }

    public async Task<TicketResponse?> GetTicketByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        ConfirmationCode target = ConfirmationCode.FromStorage(code);

        TicketRow? row = await Project(
                database.Tickets.AsNoTracking().Where(ticket => ticket.Code == target))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToResponse(row);
    }

    public async Task<OrderResponse?> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        OrderHeader? header = await database.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new OrderHeader(
                order.Id,
                order.Reference,
                order.PlacedAt,
                order.Total.AmountInMinorUnits,
                order.Total.Currency))
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        List<TicketRow> rows = await Project(
                database.Tickets.AsNoTracking().Where(ticket => ticket.OrderId == orderId))
            .ToListAsync(cancellationToken);

        return new OrderResponse(
            header.Id,
            header.Reference.Value,
            header.PlacedAt,
            header.TotalAmountMinorUnits / MinorUnitsPerUnit,
            header.TotalCurrency,
            rows.OrderBy(row => row.Seat).Select(ToResponse).ToList());
    }

    private static IQueryable<TicketRow> Project(IQueryable<Ticket> tickets) =>
        tickets.Select(ticket => new TicketRow(
            ticket.Id,
            ticket.OrderId,
            ticket.Code,
            ticket.Status,
            ticket.Seat,
            ticket.PricePaid.AmountInMinorUnits,
            ticket.PricePaid.Currency,
            ticket.IssuedAt,
            ticket.CheckedInAt,
            ticket.ShowtimeId,
            ticket.Showtime!.StartsAt,
            ticket.Showtime!.Movie!.Title,
            ticket.Showtime!.Movie!.PosterUrl,
            ticket.Showtime!.Auditorium!.Cinema!.Name,
            ticket.Showtime!.Auditorium!.Name));

    private static TicketResponse ToResponse(TicketRow row) => new(
        row.Id,
        row.Code.Value,
        row.Status.ToString(),
        row.Seat.Label,
        row.PricePaidAmountMinorUnits / MinorUnitsPerUnit,
        row.PricePaidCurrency,
        row.IssuedAt,
        row.CheckedInAt,
        row.ShowtimeId,
        row.StartsAt,
        row.MovieTitle,
        row.PosterUrl,
        row.CinemaName,
        row.AuditoriumName);

    private sealed record TicketRow(
        Guid Id,
        Guid OrderId,
        ConfirmationCode Code,
        TicketStatus Status,
        SeatNumber Seat,
        long PricePaidAmountMinorUnits,
        string PricePaidCurrency,
        DateTimeOffset IssuedAt,
        DateTimeOffset? CheckedInAt,
        Guid ShowtimeId,
        DateTimeOffset StartsAt,
        string MovieTitle,
        string PosterUrl,
        string CinemaName,
        string AuditoriumName);

    private sealed record OrderHeader(
        Guid Id,
        ConfirmationCode Reference,
        DateTimeOffset PlacedAt,
        long TotalAmountMinorUnits,
        string TotalCurrency);
}
