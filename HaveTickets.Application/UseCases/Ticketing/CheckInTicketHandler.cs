using HaveTickets.Application.Abstractions;
using HaveTickets.Contracts.V1;
using HaveTickets.Domain.Common;
using HaveTickets.Domain.ValueObjects;

namespace HaveTickets.Application.UseCases.Ticketing;

public sealed record CheckInTicketCommand(string Code);

/// <summary>
/// Redeems a ticket at the door.
/// <para>
/// A refusal is a normal outcome of a scan, not a fault, so this use case answers
/// with a structured verdict rather than an error: an operator staring at a
/// turnstile needs to know <em>why</em> the code bounced, and a scanner needs one
/// response shape it can always render.
/// </para>
/// </summary>
public sealed class CheckInTicketHandler(
    ITicketRepository tickets,
    ITicketReadModel ticketReadModel,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public const string AdmittedOutcome = "admitted";

    public async Task<Result<CheckInResponse>> HandleAsync(
        CheckInTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<ConfirmationCode> code = ConfirmationCode.Create(command.Code);
        if (code.IsFailure)
        {
            return Refused(code.Error, ticket: null);
        }

        TicketWithShowtime? found = await tickets.GetByCodeAsync(code.Value.Value, cancellationToken);
        if (found is null)
        {
            return Refused(DomainErrors.Ticket.NotFoundByCode, ticket: null);
        }

        Result checkedIn = found.Ticket.CheckIn(found.Showtime, clock.GetUtcNow());
        if (checkedIn.IsFailure)
        {
            TicketResponse? refusedTicket = await ticketReadModel.GetTicketByCodeAsync(
                code.Value.Value,
                cancellationToken);

            return Refused(checkedIn.Error, refusedTicket);
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        TicketResponse? admittedTicket = await ticketReadModel.GetTicketByCodeAsync(
            code.Value.Value,
            cancellationToken);

        return new CheckInResponse(
            Admitted: true,
            Outcome: AdmittedOutcome,
            Message: "Welcome in. Enjoy the film.",
            Ticket: admittedTicket);
    }

    private static Result<CheckInResponse> Refused(Error error, TicketResponse? ticket) =>
        Result.Success(new CheckInResponse(
            Admitted: false,
            Outcome: error.Code,
            Message: error.Description,
            Ticket: ticket));
}
