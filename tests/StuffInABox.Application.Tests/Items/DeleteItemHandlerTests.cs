using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Items.Commands.DeleteItem;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Items;

public class DeleteItemHandlerTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    [Fact]
    public async Task Handle_DeletesItem_AndPhoto()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Jacka");
        item.SetPhoto("p.jpg", 0);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _boxRepo.Setup(r => r.GetByNumberAsync(item.BoxNumber, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(new BoxNumber(1), _spaceId, _userId, "Box"));
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);

        var handler = new DeleteItemCommandHandler(_itemRepo.Object, _boxRepo.Object, _storage.Object, _access.Object);
        await handler.Handle(new DeleteItemCommand(item.Id), default);

        _storage.Verify(s => s.DeleteAsync("p.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
