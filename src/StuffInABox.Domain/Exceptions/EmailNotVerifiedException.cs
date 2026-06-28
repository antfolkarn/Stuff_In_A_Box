namespace StuffInABox.Domain.Exceptions;

/// <summary>
/// Thrown when an action requires a verified email address but the current user's
/// email isn't verified yet. Surfaced to the client as 403 with code "email_not_verified".
/// </summary>
public class EmailNotVerifiedException(string message = "E-postadressen är inte verifierad.")
    : Exception(message);
