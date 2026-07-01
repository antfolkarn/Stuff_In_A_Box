using MediatR;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Application.Account.Commands.DeleteAccount;

/// <summary>
/// GDPR right to erasure: permanently deletes the current user and all their data.
/// The cascade lives in <see cref="IAccountDeletionService"/> so the admin "delete user"
/// operation runs the exact same steps.
/// </summary>
public sealed record DeleteAccountCommand : IRequest;

public sealed class DeleteAccountCommandHandler(
    ICurrentUserService currentUser,
    IAccountDeletionService deletion)
    : IRequestHandler<DeleteAccountCommand>
{
    public Task Handle(DeleteAccountCommand request, CancellationToken ct) =>
        deletion.DeleteAsync(currentUser.UserId, ct);
}
