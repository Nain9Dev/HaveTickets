namespace HaveTickets.Domain.Common;

/// <summary>
/// Classifies a domain error so outer layers can translate it into a transport
/// concern (HTTP status, UI message) without inspecting the error code.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unexpected
}
