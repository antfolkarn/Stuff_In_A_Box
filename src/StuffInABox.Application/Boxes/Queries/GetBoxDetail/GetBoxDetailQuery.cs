using MediatR;

namespace StuffInABox.Application.Boxes.Queries.GetBoxDetail;

// SpaceId is optional: when present we resolve via space access (owner or member);
// when absent (e.g. a QR deep link to your own box) we resolve by the current user.
public sealed record GetBoxDetailQuery(int BoxNumber, Guid? SpaceId) : IRequest<BoxDetailDto?>;

public sealed record BoxDetailDto(int Number, string Label, Guid SpaceId);
