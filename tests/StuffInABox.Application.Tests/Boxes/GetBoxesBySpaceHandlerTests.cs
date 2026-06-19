using Moq;
using StuffInABox.Application.Boxes.Queries.GetBoxesBySpace;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Boxes;

public class GetBoxesBySpaceHandlerTests
{
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public GetBoxesBySpaceHandlerTests()
    {
        _user.Setup(u => u.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_ReturnsBoxesOrderedByNumber_WithItemCounts()
    {
        var box2 = Box.Create(new BoxNumber(2), _spaceId, _userId, "Verktyg");
        var box1 = Box.Create(new BoxNumber(1), _spaceId, _userId, "Vinter");

        _boxRepo.Setup(r => r.GetBySpaceAsync(_spaceId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { box2, box1 });

        _itemRepo.Setup(r => r.GetByBoxAsync(box1.Number, _userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { Item.Create(box1.Number, _userId, "Mössa") });
        _itemRepo.Setup(r => r.GetByBoxAsync(box2.Number, _userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Array.Empty<Item>());

        var handler = new GetBoxesBySpaceQueryHandler(_boxRepo.Object, _itemRepo.Object, _user.Object);
        var result = await handler.Handle(new GetBoxesBySpaceQuery(_spaceId), default);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Number);   // ordered ascending
        Assert.Equal(2, result[1].Number);
        Assert.Equal(1, result[0].ItemCount);
        Assert.Equal(0, result[1].ItemCount);
    }
}
