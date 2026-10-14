using HaveTickets.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HaveTickets.Infrastructure.Persistence.Converters;

/// <summary>Stores a seat as its canonical label, for example C7.</summary>
public sealed class SeatNumberConverter() : ValueConverter<SeatNumber, string>(
    seat => seat.Label,
    label => SeatNumber.FromLabel(label));

/// <summary>Stores a confirmation code as its eight-character value.</summary>
public sealed class ConfirmationCodeConverter() : ValueConverter<ConfirmationCode, string>(
    code => code.Value,
    value => ConfirmationCode.FromStorage(value));
