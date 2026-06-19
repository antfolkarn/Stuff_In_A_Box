using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Spaces.Commands.CreateSpace;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Spaces;

public class CreateSpaceHandlerTests
{
    private readonly Mock<ISpaceRepository> _repo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public CreateSpaceHandlerTests()
    {
        _user.Setup(u => u.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAndReturnsSpace()
    {
        Space? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Space>(), It.IsAny<CancellationToken>()))
             .Callback<Space, CancellationToken>((s, _) => saved = s)
             .Returns(Task.CompletedTask);

        var handler = new CreateSpaceCommandHandler(_repo.Object, _user.Object);
        var result = await handler.Handle(new CreateSpaceCommand("Garage", "ti-car"), default);

        Assert.NotEqual(Guid.Empty, result.SpaceId);
        Assert.Equal("Garage", result.Name);
        Assert.Equal("GAR", result.Code);
        Assert.Equal("ti-car", result.Icon);
        Assert.NotNull(saved);
        _repo.Verify(r => r.AddAsync(It.IsAny<Space>(), default), Times.Once);
    }
}
