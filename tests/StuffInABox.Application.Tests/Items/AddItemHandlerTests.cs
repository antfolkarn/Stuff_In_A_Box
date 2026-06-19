using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Items.Commands.AddItem;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Items;

public class AddItemHandlerTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly Mock<IEnrichmentQueue> _queue = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly BoxNumber _boxNum = new(3);

    public AddItemHandlerTests()
    {
        _user.Setup(u => u.UserId).Returns(_userId);
        _boxRepo.Setup(r => r.GetByNumberAsync(_boxNum, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(_boxNum, Guid.NewGuid(), _userId, "Verktyg"));
    }

    [Fact]
    public async Task Handle_SavesItem_WithTokenizerTags()
    {
        Item? saved = null;
        _itemRepo.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                 .Callback<Item, CancellationToken>((i, _) => saved = i)
                 .Returns(Task.CompletedTask);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _user.Object, _queue.Object);
        var result = await handler.Handle(new AddItemCommand(3, "Hammare"), default);

        Assert.Equal("Hammare", result.Name);
        Assert.NotEqual(Guid.Empty, result.ItemId);
        Assert.NotNull(saved);
        Assert.Contains("hammare", result.Tags);
    }

    [Fact]
    public async Task Handle_EnqueuesEnrichment()
    {
        _itemRepo.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _user.Object, _queue.Object);
        await handler.Handle(new AddItemCommand(3, "Hammare"), default);

        _queue.Verify(q => q.EnqueueEnrichment(It.IsAny<Guid>(), "Hammare"), Times.Once);
    }

    [Fact]
    public async Task Handle_BoxNotFound_Throws()
    {
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Box?)null);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _user.Object, _queue.Object);

        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => handler.Handle(new AddItemCommand(99, "Test"), default));
    }
}
