namespace HaveTickets.Domain.Common;

/// <summary>
/// The complete catalogue of errors this domain can produce.
/// <para>
/// Codes are a published contract. Adding a code is a minor change; renaming or
/// removing one is breaking and requires an ADR.
/// </para>
/// </summary>
public static class DomainErrors
{
    public static class Money
    {
        public static Error NegativeAmount => Error.Validation(
            "money.negative_amount",
            "A monetary amount cannot be negative.");

        public static Error TooManyDecimals => Error.Validation(
            "money.too_many_decimals",
            "A monetary amount supports at most two decimal places.");

        public static Error InvalidCurrency => Error.Validation(
            "money.invalid_currency",
            "Currency must be a three-letter ISO 4217 code.");

        public static Error CurrencyMismatch => Error.Validation(
            "money.currency_mismatch",
            "Amounts in different currencies cannot be combined.");
    }

    public static class Seat
    {
        public static Error RowOutOfRange => Error.Validation(
            "seat.row_out_of_range",
            "Seat row must be between 1 and 26 (A to Z).");

        public static Error NumberOutOfRange => Error.Validation(
            "seat.number_out_of_range",
            "Seat number must be between 1 and 99.");

        public static Error InvalidLabel => Error.Validation(
            "seat.invalid_label",
            "Seat label must be a row letter followed by a number, for example 'C7'.");

        public static Error NotInAuditorium(string label) => Error.Validation(
            "seat.not_in_auditorium",
            $"Seat '{label}' does not exist in this auditorium.");

        public static Error Duplicated(string label) => Error.Validation(
            "seat.duplicated",
            $"Seat '{label}' appears more than once in the same order.");

        public static Error AlreadyTaken(string label) => Error.Conflict(
            "seat.already_taken",
            $"Seat '{label}' has already been sold for this showtime.");
    }

    public static class Movie
    {
        public static Error TitleRequired => Error.Validation(
            "movie.title_required",
            "A movie must have a title.");

        public static Error InvalidDuration => Error.Validation(
            "movie.invalid_duration",
            "A movie must last between 1 and 600 minutes.");

        public static Error NotFound(Guid id) => Error.NotFound(
            "movie.not_found",
            $"No movie exists with identifier '{id}'.");
    }

    public static class Auditorium
    {
        public static Error NameRequired => Error.Validation(
            "auditorium.name_required",
            "An auditorium must have a name.");

        public static Error InvalidLayout => Error.Validation(
            "auditorium.invalid_layout",
            "An auditorium must have between 1 and 26 rows and between 1 and 99 seats per row.");
    }

    public static class Cinema
    {
        public static Error NameRequired => Error.Validation(
            "cinema.name_required",
            "A cinema must have a name.");
    }

    public static class Showtime
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "showtime.not_found",
            $"No showtime exists with identifier '{id}'.");

        public static Error SalesClosed => Error.Conflict(
            "showtime.sales_closed",
            "Sales for this showtime are closed.");

        public static Error StartsInThePast => Error.Validation(
            "showtime.starts_in_the_past",
            "A showtime cannot be scheduled in the past.");
    }

    public static class Order
    {
        public static Error HolderRequired => Error.Validation(
            "order.holder_required",
            "An order must be placed by an identified holder.");

        public static Error NoSeatsSelected => Error.Validation(
            "order.no_seats_selected",
            "An order must contain at least one seat.");

        public static Error TooManySeats(int max) => Error.Validation(
            "order.too_many_seats",
            $"An order may contain at most {max} seats.");

        public static Error NotFound(Guid id) => Error.NotFound(
            "order.not_found",
            $"No order exists with identifier '{id}'.");
    }

    public static class Ticket
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "ticket.not_found",
            $"No ticket exists with identifier '{id}'.");

        public static Error NotFoundByCode => Error.NotFound(
            "ticket.not_found_by_code",
            "No ticket matches that confirmation code.");

        public static Error AlreadyCheckedIn => Error.Conflict(
            "ticket.already_checked_in",
            "This ticket has already been used to enter.");

        public static Error Cancelled => Error.Conflict(
            "ticket.cancelled",
            "This ticket was cancelled and is no longer valid.");

        public static Error CheckInNotOpenYet => Error.Conflict(
            "ticket.check_in_not_open_yet",
            "Check-in opens one hour before the showtime starts.");

        public static Error CheckInClosed => Error.Conflict(
            "ticket.check_in_closed",
            "Check-in closed 30 minutes after the showtime started.");

        public static Error TooLateToCancel => Error.Conflict(
            "ticket.too_late_to_cancel",
            "A ticket can only be cancelled while sales for its showtime are open.");

        public static Error NotOwnedByHolder => Error.Forbidden(
            "ticket.not_owned_by_holder",
            "This ticket belongs to a different holder.");
    }

    public static class ConfirmationCode
    {
        public static Error InvalidFormat => Error.Validation(
            "confirmation_code.invalid_format",
            "A confirmation code is 8 characters long and uses the alphabet 0-9 A-Z excluding I, L, O and U.");
    }
}
