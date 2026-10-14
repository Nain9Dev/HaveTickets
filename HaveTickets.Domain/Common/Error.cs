namespace HaveTickets.Domain.Common;

/// <summary>
/// An immutable, machine-readable failure descriptor.
/// <para>
/// <see cref="Code"/> is part of the public contract: clients may branch on it,
/// so codes are never renamed once released. <see cref="Description"/> is for
/// humans and may change freely.
/// </para>
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorType.Unexpected);

    public override string ToString() => $"{Code}: {Description}";
}
