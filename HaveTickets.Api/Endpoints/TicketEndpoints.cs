using HaveTickets.Api.Extensions;
using HaveTickets.Application.Abstractions;
using HaveTickets.Application.UseCases.Ticketing;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;

namespace HaveTickets.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder group = routes.MapGroup("/api/v1").WithTags("Ticketing");

        group.MapPost("/orders", async (
                PurchaseTicketsRequest request,
                PurchaseTicketsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<OrderResponse> result = await handler.HandleAsync(
                    new PurchaseTicketsCommand(request.ShowtimeId, request.HolderId, request.Seats),
                    cancellationToken);

                return result.ToHttpResult(
                    httpContext,
                    order => Results.Created($"/api/v1/orders/{order.Id}", order));
            })
            .WithName("PurchaseTickets")
            .WithSummary("Buys one or more seats for a showtime.")
            .WithDescription("Returns 409 with errorCode seat.already_taken when another buyer took a seat first.")
            .RequireRateLimiting(ServiceCollectionExtensions.WritePolicy)
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/tickets", async (
                Guid? holderId,
                ListTicketsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<TicketResponse>> result = await handler.HandleAsync(
                    new ListTicketsQuery(holderId ?? Guid.Empty),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .WithName("ListTickets")
            .WithSummary("Lists every ticket belonging to a holder.")
            .RequireRateLimiting(ServiceCollectionExtensions.ReadPolicy)
            .Produces<IReadOnlyList<TicketResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/tickets/{ticketId:guid}/qr", async (
                Guid ticketId,
                ITicketReadModel tickets,
                IQrCodeGenerator qrCodes,
                CancellationToken cancellationToken) =>
            {
                TicketResponse? ticket = await tickets.GetTicketAsync(ticketId, cancellationToken);
                if (ticket is null)
                {
                    return Results.NotFound();
                }

                byte[] png = qrCodes.GeneratePng(ticket.Code);
                return Results.File(png, "image/png", $"ticket-{ticket.Code}.png");
            })
            .WithName("GetTicketQrCode")
            .WithSummary("Renders the ticket confirmation code as a scannable PNG.")
            .RequireRateLimiting(ServiceCollectionExtensions.ReadPolicy)
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/tickets/{ticketId:guid}", async (
                Guid ticketId,
                Guid holderId,
                CancelTicketHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.HandleAsync(
                    new CancelTicketCommand(ticketId, holderId),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .WithName("CancelTicket")
            .WithSummary("Cancels a ticket and releases its seat.")
            .RequireRateLimiting(ServiceCollectionExtensions.WritePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/tickets/check-in", async (
                CheckInRequest request,
                CheckInTicketHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                Result<CheckInResponse> result = await handler.HandleAsync(
                    new CheckInTicketCommand(request.Code),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .WithName("CheckInTicket")
            .WithSummary("Redeems a ticket at the door.")
            .WithDescription("A refusal is a scan outcome, not a transport error: this always answers 200 with admitted=false and a reason.")
            .RequireRateLimiting(ServiceCollectionExtensions.WritePolicy)
            .Produces<CheckInResponse>();

        return routes;
    }
}
