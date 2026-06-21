using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Labels.Queries;

public sealed class GetLabelDataQueryHandler(
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    ISpaceAccessService access)
    : IRequestHandler<GetLabelDataQuery, IReadOnlyList<LabelDto>>
{
    public async Task<IReadOnlyList<LabelDto>> Handle(GetLabelDataQuery request, CancellationToken ct)
    {
        var spaces = await access.GetAccessibleSpacesAsync(ct);
        var result = new List<LabelDto>();

        foreach (var space in spaces)
        {
            if (request.SpaceId.HasValue && space.Id != request.SpaceId.Value)
                continue;

            var boxes = await boxRepo.GetBySpaceAsync(space.Id, space.OwnerId, ct);
            foreach (var box in boxes)
            {
                if (request.BoxNumber.HasValue && box.Number.Value != request.BoxNumber.Value)
                    continue;

                var items = await itemRepo.GetByBoxAsync(box.Number, space.OwnerId, ct);
                result.Add(new LabelDto(
                    box.Number.Value,
                    box.Label,
                    space.Name,
                    items.Take(6).Select(i => i.Name).ToList()));
            }
        }

        return result;
    }
}
