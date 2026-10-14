using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HaveTickets.Domain.Common;

namespace HaveTickets.Domain.ValueObjects;

/// <summary>
/// A seat position inside an auditorium, rendered as a row letter plus a number
/// (for example C7). Rows are 1-based so that row 1 maps to A.
/// </summary>
public sealed record SeatNumber : IComparable<SeatNumber>
{
    public const int MaxRow = 26;
    public const int MaxNumber = 99;

    private SeatNumber(int row, int number)
    {
        Row = row;
        Number = number;
    }

    /// <summary>1-based row index; row 1 is A.</summary>
    public int Row { get; private init; }

    /// <summary>1-based seat index within the row.</summary>
    public int Number { get; private init; }

    /// <summary>Canonical label, for example C7. This is what gets persisted.</summary>
    public string Label => string.Create(
        CultureInfo.InvariantCulture,
        $"{(char)('A' + Row - 1)}{Number}");

    public static Result<SeatNumber> Create(int row, int number)
    {
        if (row is < 1 or > MaxRow)
        {
            return DomainErrors.Seat.RowOutOfRange;
        }

        if (number is < 1 or > MaxNumber)
        {
            return DomainErrors.Seat.NumberOutOfRange;
        }

        return new SeatNumber(row, number);
    }

    /// <summary>Parses a canonical label such as C7. Case-insensitive.</summary>
    public static Result<SeatNumber> Parse(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return DomainErrors.Seat.InvalidLabel;
        }

        ReadOnlySpan<char> trimmed = label.AsSpan().Trim();
        if (trimmed.Length is < 2 or > 3 || !char.IsAsciiLetter(trimmed[0]))
        {
            return DomainErrors.Seat.InvalidLabel;
        }

        if (!int.TryParse(trimmed[1..], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
        {
            return DomainErrors.Seat.InvalidLabel;
        }

        int row = char.ToUpperInvariant(trimmed[0]) - 'A' + 1;
        return Create(row, number);
    }

    public static bool TryParse(string? label, [NotNullWhen(true)] out SeatNumber? seat)
    {
        Result<SeatNumber> result = Parse(label);
        seat = result.IsSuccess ? result.Value : null;
        return result.IsSuccess;
    }

    /// <summary>Rebuilds a seat from already-validated storage. Persistence layer only.</summary>
    public static SeatNumber FromLabel(string label)
    {
        Result<SeatNumber> result = Parse(label);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Corrupt seat label in storage: {label}");
    }

    public int CompareTo(SeatNumber? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byRow = Row.CompareTo(other.Row);
        return byRow != 0 ? byRow : Number.CompareTo(other.Number);
    }

    public static bool operator <(SeatNumber? left, SeatNumber? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(SeatNumber? left, SeatNumber? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(SeatNumber? left, SeatNumber? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(SeatNumber? left, SeatNumber? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;

    public override string ToString() => Label;
}
