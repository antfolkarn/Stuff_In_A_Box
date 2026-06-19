using MediatR;

namespace StuffInABox.Application.Labels.Queries;

public sealed record GetLabelDataQuery(Guid? SpaceId = null, int? BoxNumber = null)
    : IRequest<IReadOnlyList<LabelDto>>;

public sealed record LabelDto(
    int BoxNumber,
    string BoxLabel,
    string SpaceName,
    IReadOnlyList<string> ItemNames);
