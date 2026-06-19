namespace StuffInABox.Domain.Exceptions;

public class ForbiddenException(string message = "Access denied.")
    : Exception(message);
