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
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly Mock<IEntitlementService> _entitlements = new();
    private readonly Mock<IEnrichmentQueue> _queue = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly BoxNumber _boxNum = new(3);
    private readonly Guid _spaceId = Guid.NewGuid();

    public AddItemHandlerTests()
    {
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);
        _boxRepo.Setup(r => r.GetByNumberAsync(_boxNum, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(_boxNum, _spaceId, _userId, "Verktyg"));
    }

    [Fact]
    public async Task Handle_SavesItem_WithTokenizerTags()
    {
        Item? saved = null;
        _itemRepo.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                 .Callback<Item, CancellationToken>((i, _) => saved = i)
                 .Returns(Task.CompletedTask);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object, _entitlements.Object, _queue.Object);
        var result = await handler.Handle(new AddItemCommand(3, _spaceId, "Hammare"), default);

        Assert.Equal("Hammare", result.Name);
        Assert.NotEqual(Guid.Empty, result.ItemId);
        Assert.NotNull(saved);
        Assert.Contains("hammare", result.Tags);
    }

    [Fact]
    public async Task Handle_MergesPhotoTags_WithTokenizerTags()
    {
        Item? saved = null;
        _itemRepo.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                 .Callback<Item, CancellationToken>((i, _) => saved = i)
                 .Returns(Task.CompletedTask);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object, _entitlements.Object, _queue.Object);
        var result = await handler.Handle(
            new AddItemCommand(3, _spaceId, "Röd jacka", new[] { "jacka", "röd", "ytterkläder" }), default);

        // Tokenizer tags from the name plus the photo-derived tags, de-duplicated
        Assert.Contains("röd", result.Tags);
        Assert.Contains("jacka", result.Tags);
        Assert.Contains("ytterkläder", result.Tags);
        Assert.Equal(result.Tags.Count, result.Tags.Distinct().Count());
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Handle_EnqueuesEnrichment()
    {
        _itemRepo.Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object, _entitlements.Object, _queue.Object);
        await handler.Handle(new AddItemCommand(3, _spaceId, "Hammare"), default);

        _queue.Verify(q => q.EnqueueEnrichment(It.IsAny<Guid>(), "Hammare"), Times.Once);
    }

    [Fact]
    public async Task Handle_BoxNotFound_Throws()
    {
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Box?)null);

        var handler = new AddItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object, _entitlements.Object, _queue.Object);

        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => handler.Handle(new AddItemCommand(99, _spaceId, "Test"), default));
    }
}
