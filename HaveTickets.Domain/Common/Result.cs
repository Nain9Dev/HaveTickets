namespace HaveTickets.Domain.Common;

/// <summary>
/// Outcome of an operation that can fail for expected, modelled reasons.
/// <para>
/// Business rule violations are values, not exceptions: they are part of the
/// contract, they are exhaustively testable, and they cost nothing to produce.
/// Exceptions remain reserved for genuinely unexpected faults.
/// </para>
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Returns the first failure in <paramref name="results"/>, or success.</summary>
    public static Result FirstFailureOrSuccess(params Result[] results)
    {
        foreach (Result result in results)
        {
            if (result.IsFailure)
            {
                return Failure(result.Error);
            }
        }

        return Success();
    }

    public static implicit operator Result(Error error) => Failure(error);
}

/// <inheritdoc cref="Result"/>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The produced value. Only valid when <see cref="Result.IsSuccess"/>.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
