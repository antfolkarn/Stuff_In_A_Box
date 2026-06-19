using Moq;
using StuffInABox.Application.Boxes.Queries.GetBoxDetail;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Boxes;

public class GetBoxDetailHandlerTests
{
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public GetBoxDetailHandlerTests()
    {
        _user.Setup(u => u.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_ExistingBox_ReturnsDetail()
    {
        var spaceId = Guid.NewGuid();
        var box = Box.Create(new BoxNumber(8), spaceId, _userId, "Julpynt");
        _boxRepo.Setup(r => r.GetByNumberAsync(It.Is<BoxNumber>(n => n.Value == 8), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(box);

        var handler = new GetBoxDetailQueryHandler(_boxRepo.Object, _user.Object);
        var result = await handler.Handle(new GetBoxDetailQuery(8), default);

        Assert.NotNull(result);
        Assert.Equal(8, result!.Number);
        Assert.Equal("Julpynt", result.Label);
        Assert.Equal(spaceId, result.SpaceId);
    }

    [Fact]
    public async Task Handle_MissingBox_ReturnsNull()
    {
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Box?)null);

        var handler = new GetBoxDetailQueryHandler(_boxRepo.Object, _user.Object);
        var result = await handler.Handle(new GetBoxDetailQuery(99), default);

        Assert.Null(result);
    }
}
