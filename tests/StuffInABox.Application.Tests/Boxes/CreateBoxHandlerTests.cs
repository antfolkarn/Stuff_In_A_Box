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
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public CreateBoxHandlerTests()
    {
        _boxRepo.Setup(r => r.GetNextBoxNumberAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BoxNumber(5));
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);
    }

    [Fact]
    public async Task Handle_AllocatesNextBoxNumber()
    {
        Box? saved = null;
        _boxRepo.Setup(r => r.AddAsync(It.IsAny<Box>(), It.IsAny<CancellationToken>()))
                .Callback<Box, CancellationToken>((b, _) => saved = b)
                .Returns(Task.CompletedTask);

        var handler = new CreateBoxCommandHandler(_boxRepo.Object, _access.Object);
        var result = await handler.Handle(new CreateBoxCommand(_spaceId, "Verktyg"), default);

        Assert.Equal(5, result.BoxNumber);
        Assert.Equal("Verktyg", result.Label);
        Assert.NotNull(saved);
        Assert.Equal(5, saved!.Number.Value);
        Assert.Equal(_spaceId, saved.SpaceId);
    }

    [Fact]
    public async Task Handle_NoAccess_Throws()
    {
        var otherSpace = Guid.NewGuid();
        _access.Setup(a => a.RequireSpaceAsync(otherSpace, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Domain.Exceptions.ForbiddenException());

        var handler = new CreateBoxCommandHandler(_boxRepo.Object, _access.Object);

        await Assert.ThrowsAsync<Domain.Exceptions.ForbiddenException>(
            () => handler.Handle(new CreateBoxCommand(otherSpace, "Test"), default));
    }
}
