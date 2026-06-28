using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Common.Behaviors;

/// <summary>
/// Blocks commands marked with <see cref="IRequireVerifiedEmail"/> when the current
/// user's email isn't verified. OAuth identities (and grandfathered accounts) pass via
/// <see cref="Domain.Entities.UserIdentity.IsEmailVerified"/>. Throws
/// <see cref="EmailNotVerifiedException"/> (→ 403 "email_not_verified") otherwise.
/// </summary>
public sealed class EmailVerificationBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IUserIdentityRepository identities)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IRequireVerifiedEmail)
        {
            var identity = await identities.FindByIdAsync(currentUser.UserId.Value, ct);
            if (identity is null || !identity.IsEmailVerified)
                throw new EmailNotVerifiedException();
        }

        return await next(ct);
    }
}
