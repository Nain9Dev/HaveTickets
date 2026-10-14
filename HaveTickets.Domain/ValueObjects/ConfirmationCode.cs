using System.Security.Cryptography;
using HaveTickets.Domain.Common;

namespace HaveTickets.Domain.ValueObjects;

/// <summary>
/// A short, human-readable booking reference.
/// <para>
/// Uses Crockford-style Base32 (no I, L, O or U) so a code read aloud at a box
/// office cannot be confused with 1, 0 or V. Eight characters over a 32-symbol
/// alphabet give 2^40 combinations, drawn from a cryptographic source.
/// </para>
/// </summary>
public sealed record ConfirmationCode
{
    public const int Length = 8;
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private ConfirmationCode(string value) => Value = value;

    public string Value { get; private init; }

    public static ConfirmationCode New()
    {
        Span<char> buffer = stackalloc char[Length];
        for (int i = 0; i < Length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new ConfirmationCode(new string(buffer));
    }

    public static Result<ConfirmationCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainErrors.ConfirmationCode.InvalidFormat;
        }

        string normalised = value.Trim().ToUpperInvariant();
        if (normalised.Length != Length)
        {
            return DomainErrors.ConfirmationCode.InvalidFormat;
        }

        foreach (char character in normalised)
        {
            if (!Alphabet.Contains(character, StringComparison.Ordinal))
            {
                return DomainErrors.ConfirmationCode.InvalidFormat;
            }
        }

        return new ConfirmationCode(normalised);
    }

    /// <summary>Rebuilds a code from already-validated storage. Persistence layer only.</summary>
    public static ConfirmationCode FromStorage(string value) => new(value);

    public override string ToString() => Value;
}
