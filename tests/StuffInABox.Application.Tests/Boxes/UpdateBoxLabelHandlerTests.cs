using Moq;
using StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Boxes;

public class UpdateBoxLabelHandlerTests
{
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public UpdateBoxLabelHandlerTests()
    {
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);
    }

    [Fact]
    public async Task Handle_RenamesBox()
    {
        var box = Box.Create(new BoxNumber(3), _spaceId, _userId, "Gammalt namn");
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(box);
        Box? updated = null;
        _boxRepo.Setup(r => r.UpdateAsync(It.IsAny<Box>(), It.IsAny<CancellationToken>()))
                .Callback<Box, CancellationToken>((b, _) => updated = b)
                .Returns(Task.CompletedTask);

        var handler = new UpdateBoxLabelCommandHandler(_boxRepo.Object, _access.Object);
        await handler.Handle(new UpdateBoxLabelCommand(3, _spaceId, "Nytt namn"), default);

        Assert.Equal("Nytt namn", updated!.Label);
    }

    [Fact]
    public async Task Handle_MissingBox_Throws()
    {
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Box?)null);
        var handler = new UpdateBoxLabelCommandHandler(_boxRepo.Object, _access.Object);

        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => handler.Handle(new UpdateBoxLabelCommand(99, _spaceId, "x"), default));
    }
}
