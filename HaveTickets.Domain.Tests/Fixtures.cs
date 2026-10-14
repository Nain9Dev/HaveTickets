using HaveTickets.Domain.Common;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.ValueObjects;
using HaveTickets.Domain.Venues;
using Shouldly;

namespace HaveTickets.Domain.Tests;

/// <summary>Shared builders so each test states only what it cares about.</summary>
internal static class Fixtures
{
    /// <summary>A fixed clock reading: tests never depend on the machine time.</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid Holder = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid OtherHolder = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static Auditorium CreateAuditorium(int rows = 10, int seatsPerRow = 12) =>
        Auditorium.Create(Guid.NewGuid(), Guid.NewGuid(), "Room 1", rows, seatsPerRow).Value;

    public static Showtime CreateShowtime(Auditorium auditorium, TimeSpan startsIn, decimal price = 9.50m) =>
        Showtime.Reconstitute(
            Guid.NewGuid(),
            Guid.NewGuid(),
            auditorium.Id,
            Now + startsIn,
            Money.Create(price).Value);

    public static IReadOnlyList<SeatNumber> Seats(params string[] labels) =>
        [.. labels.Select(label => SeatNumber.Parse(label).Value)];

    public static IReadOnlySet<string> Occupied(params string[] labels) =>
        new HashSet<string>(labels, StringComparer.Ordinal);
}

internal static class ResultAssertions
{
    public static void ShouldSucceed(this Result result) =>
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);

    public static void ShouldFailWith(this Result result, string errorCode)
    {
        result.IsFailure.ShouldBeTrue("The operation was expected to fail.");
        result.Error.Code.ShouldBe(errorCode);
    }

    public static void ShouldFailWith(this Result result, Error expected)
    {
        result.IsFailure.ShouldBeTrue("The operation was expected to fail.");
        result.Error.ShouldBe(expected);
    }
}
