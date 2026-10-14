using System.Globalization;
using HaveTickets.Domain.Common;

namespace HaveTickets.Domain.ValueObjects;

/// <summary>
/// A monetary amount stored as integer minor units (cents) plus an ISO 4217 currency.
/// <para>
/// Money is never stored as a floating point number and never as a bare
/// <see cref="decimal"/> in the database: SQLite has no decimal type, so a decimal
/// column silently degrades to REAL and loses cents. Integer minor units make
/// arithmetic exact and comparisons deterministic across every provider.
/// </para>
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "EUR";
    private const int MinorUnitsPerUnit = 100;

    private Money(long amountInMinorUnits, string currency)
    {
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
    }

    public long AmountInMinorUnits { get; private init; }

    /// <summary>ISO 4217 alpha-3 code, upper case.</summary>
    public string Currency { get; private init; }

    /// <summary>The amount in major units. Derived; never persisted.</summary>
    public decimal Amount => decimal.Divide(AmountInMinorUnits, MinorUnitsPerUnit);

    public static Money Zero(string currency = DefaultCurrency) => new(0, currency);

    /// <summary>Validates and builds an amount from major units (for example 12.50).</summary>
    public static Result<Money> Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            return DomainErrors.Money.NegativeAmount;
        }

        if (decimal.Round(amount, 2, MidpointRounding.ToEven) != amount)
        {
            return DomainErrors.Money.TooManyDecimals;
        }

        Result<string> normalisedCurrency = NormaliseCurrency(currency);
        if (normalisedCurrency.IsFailure)
        {
            return normalisedCurrency.Error;
        }

        return new Money((long)(amount * MinorUnitsPerUnit), normalisedCurrency.Value);
    }

    /// <summary>
    /// Rebuilds an amount from already-validated storage. Used by the persistence
    /// layer only, where the invariants were enforced on the way in.
    /// </summary>
    public static Money FromMinorUnits(long amountInMinorUnits, string currency = DefaultCurrency) =>
        new(amountInMinorUnits, currency.ToUpperInvariant());

    public Result<Money> Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        return new Money(AmountInMinorUnits + other.AmountInMinorUnits, Currency);
    }

    public Money Multiply(int factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factor);
        return new Money(AmountInMinorUnits * factor, Currency);
    }

    private static Result<string> NormaliseCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            return DomainErrors.Money.InvalidCurrency;
        }

        foreach (char character in currency)
        {
            if (!char.IsAsciiLetter(character))
            {
                return DomainErrors.Money.InvalidCurrency;
            }
        }

        return currency.ToUpperInvariant();
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");
}
