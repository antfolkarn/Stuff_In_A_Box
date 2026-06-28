namespace StuffInABox.Application.Common.Behaviors;

/// <summary>
/// Marker on commands that the current user may only run once their email is verified.
/// Enforced centrally by <see cref="EmailVerificationBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface IRequireVerifiedEmail;
