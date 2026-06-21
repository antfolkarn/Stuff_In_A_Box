using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Items.Commands.UpdateItem;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Items;

public class UpdateItemHandlerTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    [Fact]
    public async Task Handle_UpdatesNameAndTags()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Gammalt");
        item.ReplaceTags(new[] { "gammal" });
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _boxRepo.Setup(r => r.GetByNumberAsync(item.BoxNumber, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(new BoxNumber(1), _spaceId, _userId, "Box"));
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);

        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object);
        await handler.Handle(new UpdateItemCommand(item.Id, "Nytt", new[] { "ny", "tagg" }), default);

        Assert.Equal("Nytt", item.Name);
        Assert.Equal(new[] { "ny", "tagg" }, item.Tags);
    }

    [Fact]
    public async Task Handle_ItemNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _itemRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Item?)null);

        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object);
        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => handler.Handle(new UpdateItemCommand(id, "x", null), default));
    }

    [Fact]
    public async Task Handle_NoAccessToSpace_Throws()
    {
        var item = Item.Create(new BoxNumber(1), new UserId(Guid.NewGuid()), "Annans");
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _boxRepo.Setup(r => r.GetByNumberAsync(item.BoxNumber, item.OwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(new BoxNumber(1), _spaceId, item.OwnerId, "Box"));
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Domain.Exceptions.ForbiddenException());

        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _access.Object);
        await Assert.ThrowsAsync<Domain.Exceptions.ForbiddenException>(
            () => handler.Handle(new UpdateItemCommand(item.Id, "x", null), default));
    }
}
