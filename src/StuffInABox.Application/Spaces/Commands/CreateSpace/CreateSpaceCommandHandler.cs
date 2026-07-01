using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Spaces.Commands.CreateSpace;

public sealed class CreateSpaceCommandHandler(
    ISpaceRepository repository,
    ICurrentUserService currentUser,
    IEntitlementService entitlements)
    : IRequestHandler<CreateSpaceCommand, CreateSpaceResult>
{
    public async Task<CreateSpaceResult> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        await entitlements.EnsureCanAddSpaceAsync(currentUser.UserId, ct);

        var space = Space.Create(currentUser.UserId, request.Name, request.Icon);
        await repository.AddAsync(space, ct);
        return new CreateSpaceResult(space.Id, space.Name, space.Code.Value, space.Icon);
    }
}
