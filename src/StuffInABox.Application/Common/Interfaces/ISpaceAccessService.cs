using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Central authorization for spaces. All content (boxes, items) is owned by the
/// space owner; invited members are collaborators. Access checks resolve the
/// effective owner so member operations run against the owner's data.
/// </summary>
public interface ISpaceAccessService
{
    /// <summary>
    /// Returns the space's owner id if the current user may act on it. With
    /// <paramref name="ownerOnly"/> only the owner passes; otherwise members pass too.
    /// Throws NotFoundException if the space is missing, ForbiddenException if no access.
    /// </summary>
    Task<UserId> RequireSpaceAsync(Guid spaceId, bool ownerOnly = false, CancellationToken ct = default);

    /// <summary>Every space the current user can see: owned plus joined-as-member.</summary>
    Task<IReadOnlyList<Space>> GetAccessibleSpacesAsync(CancellationToken ct = default);
}
