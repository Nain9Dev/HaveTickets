using System.Diagnostics;
using HaveTickets.Domain.Common;

namespace HaveTickets.Api.Extensions;

/// <summary>
/// Translates a domain <see cref="Result"/> into an HTTP response.
/// <para>
/// This is the single place where domain vocabulary becomes transport vocabulary.
/// Failures are returned as RFC 9457 problem details carrying the stable error
/// code in an <c>errorCode</c> extension member, so a client can branch on
/// <c>seat.already_taken</c> without parsing prose.
/// </para>
/// </summary>
public static class ResultExtensions
{
    private const string ErrorDocumentationBase = "https://github.com/Nain9Dev/HaveTickets/blob/master/docs/20-architecture.md#error-codes";

    public static IResult ToHttpResult<TValue>(
        this Result<TValue> result,
        HttpContext httpContext,
        Func<TValue, IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? (onSuccess is null ? Results.Ok(result.Value) : onSuccess(result.Value))
            : result.ToProblem(httpContext);
    }

    public static IResult ToHttpResult(this Result result, HttpContext httpContext, IResult? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? (onSuccess ?? Results.NoContent())
            : result.ToProblem(httpContext);
    }

    public static IResult ToProblem(this Result result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        Error error = result.Error;
        int statusCode = StatusCodeFor(error.Type);

        return Results.Problem(
            title: TitleFor(error.Type),
            detail: error.Description,
            statusCode: statusCode,
            type: $"{ErrorDocumentationBase}-{error.Code.Replace('.', '-')}",
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["errorCode"] = error.Code,
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
            });
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "The request was not valid.",
        ErrorType.NotFound => "The resource was not found.",
        ErrorType.Conflict => "The request conflicts with the current state.",
        ErrorType.Forbidden => "The request was not allowed.",
        _ => "An unexpected error occurred."
    };
}
