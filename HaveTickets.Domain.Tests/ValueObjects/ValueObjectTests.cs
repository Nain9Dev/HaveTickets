using CsCheck;
using HaveTickets.Domain.Common;
using HaveTickets.Domain.ValueObjects;
using Shouldly;

namespace HaveTickets.Domain.Tests.ValueObjects;

public sealed class SeatNumberTests
{
    [Fact]
    public void Parse_reads_a_row_letter_and_a_number()
    {
        Result<SeatNumber> result = SeatNumber.Parse("C7");

        result.ShouldSucceed();
        result.Value.Row.ShouldBe(3);
        result.Value.Number.ShouldBe(7);
        result.Value.Label.ShouldBe("C7");
    }

    [Theory]
    [InlineData("c7", "C7")]
    [InlineData("  b12 ", "B12")]
    [InlineData("z99", "Z99")]
    public void Parse_is_case_and_whitespace_tolerant(string input, string expectedLabel)
    {
        SeatNumber.Parse(input).Value.Label.ShouldBe(expectedLabel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("7")]
    [InlineData("C")]
    [InlineData("7C")]
    [InlineData("AA1")]
    [InlineData("C100")]
    [InlineData("C-1")]
    [InlineData("C0")]
    [InlineData("[1")]
    public void Parse_rejects_malformed_labels(string? input)
    {
        SeatNumber.Parse(input).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 1, "seat.row_out_of_range")]
    [InlineData(27, 1, "seat.row_out_of_range")]
    [InlineData(1, 0, "seat.number_out_of_range")]
    [InlineData(1, 100, "seat.number_out_of_range")]
    public void Create_enforces_the_bounds(int row, int number, string errorCode)
    {
        SeatNumber.Create(row, number).ShouldFailWith(errorCode);
    }

    private static readonly string[] UnsortedLabels = ["B1", "A10", "A2", "C1"];

    [Fact]
    public void Seats_sort_by_row_then_number()
    {
        SeatNumber[] sorted = [.. UnsortedLabels.Select(label => SeatNumber.Parse(label).Value).Order()];

        sorted.Select(seat => seat.Label).ShouldBe(["A2", "A10", "B1", "C1"]);
    }

    [Fact]
    public void Every_valid_seat_round_trips_through_its_label()
    {
        Gen.Int[1, SeatNumber.MaxRow]
            .Select(Gen.Int[1, SeatNumber.MaxNumber])
            .Sample((row, number) =>
            {
                SeatNumber original = SeatNumber.Create(row, number).Value;
                SeatNumber parsed = SeatNumber.Parse(original.Label).Value;

                return parsed == original;
            });
    }
}

public sealed class MoneyTests
{
    [Fact]
    public void Create_stores_the_amount_in_minor_units()
    {
        Money money = Money.Create(9.50m).Value;

        money.AmountInMinorUnits.ShouldBe(950);
        money.Amount.ShouldBe(9.50m);
        money.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Create_rejects_negative_amounts()
    {
        Money.Create(-0.01m).ShouldFailWith("money.negative_amount");
    }

    [Fact]
    public void Create_rejects_sub_cent_precision_instead_of_rounding_it()
    {
        Money.Create(1.005m).ShouldFailWith("money.too_many_decimals");
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void Create_rejects_malformed_currencies(string currency)
    {
        Money.Create(1m, currency).ShouldFailWith("money.invalid_currency");
    }

    [Fact]
    public void Create_normalises_the_currency_code()
    {
        Money.Create(1m, "usd").Value.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Add_refuses_to_mix_currencies()
    {
        Money euros = Money.Create(1m).Value;
        Money dollars = Money.Create(1m, "USD").Value;

        euros.Add(dollars).ShouldFailWith("money.currency_mismatch");
    }

    [Fact]
    public void Multiply_is_exact_for_amounts_that_binary_floats_cannot_hold()
    {
        // 0.1 x 3 is 0.30000000000000004 in double arithmetic.
        Money.Create(0.10m).Value.Multiply(3).AmountInMinorUnits.ShouldBe(30);
    }

    [Fact]
    public void Addition_is_commutative_and_has_zero_as_identity()
    {
        Gen.Long[0, 1_000_000]
            .Select(Gen.Long[0, 1_000_000])
            .Sample((a, b) =>
            {
                Money x = Money.FromMinorUnits(a);
                Money y = Money.FromMinorUnits(b);

                return x.Add(y).Value == y.Add(x).Value
                    && x.Add(Money.Zero()).Value == x;
            });
    }
}

public sealed class ConfirmationCodeTests
{
    [Fact]
    public void New_generates_a_code_the_parser_accepts()
    {
        Gen.Const(0).Sample(_ =>
        {
            ConfirmationCode generated = ConfirmationCode.New();

            return generated.Value.Length == ConfirmationCode.Length
                && ConfirmationCode.Create(generated.Value).IsSuccess;
        }, iter: 500);
    }

    [Fact]
    public void Create_normalises_case_and_whitespace()
    {
        ConfirmationCode.Create("  abcd2345 ").Value.Value.ShouldBe("ABCD2345");
    }

    [Theory]
    [InlineData("ABCD234")]
    [InlineData("ABCD23456")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_rejects_the_wrong_length(string? value)
    {
        ConfirmationCode.Create(value).ShouldFailWith("confirmation_code.invalid_format");
    }

    [Theory]
    [InlineData("ABCDEFGI")]
    [InlineData("ABCDEFGL")]
    [InlineData("ABCDEFGO")]
    [InlineData("ABCDEFGU")]
    [InlineData("ABCD-234")]
    public void Create_rejects_look_alike_and_foreign_characters(string value)
    {
        ConfirmationCode.Create(value).ShouldFailWith("confirmation_code.invalid_format");
    }
}
