using Moq;
using StuffInABox.Application.Boxes.Commands.CreateBox;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Boxes;

public class CreateBoxHandlerTests
{
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<ISpaceRepository> _spaceRepo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public CreateBoxHandlerTests()
    {
        _user.Setup(u => u.UserId).Returns(_userId);
        _boxRepo.Setup(r => r.GetNextBoxNumberAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BoxNumber(5));
        _spaceRepo.Setup(r => r.GetByIdAsync(_spaceId, _userId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Space.Create(_userId, "Garage", "ti-car"));
    }

    [Fact]
    public async Task Handle_AllocatesNextBoxNumber()
    {
        Box? saved = null;
        _boxRepo.Setup(r => r.AddAsync(It.IsAny<Box>(), It.IsAny<CancellationToken>()))
                .Callback<Box, CancellationToken>((b, _) => saved = b)
                .Returns(Task.CompletedTask);

        var handler = new CreateBoxCommandHandler(_boxRepo.Object, _spaceRepo.Object, _user.Object);
        var result = await handler.Handle(new CreateBoxCommand(_spaceId, "Verktyg"), default);

        Assert.Equal(5, result.BoxNumber);
        Assert.Equal("Verktyg", result.Label);
        Assert.NotNull(saved);
        Assert.Equal(5, saved!.Number.Value);
    }

    [Fact]
    public async Task Handle_SpaceNotFound_Throws()
    {
        _spaceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Space?)null);

        var handler = new CreateBoxCommandHandler(_boxRepo.Object, _spaceRepo.Object, _user.Object);

        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => handler.Handle(new CreateBoxCommand(Guid.NewGuid(), "Test"), default));
    }
}
