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
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public DeleteItemHandlerTests() => _user.Setup(u => u.UserId).Returns(_userId);

    [Fact]
    public async Task Handle_DeletesItem_AndPhoto()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Jacka");
        item.SetPhoto("p.jpg");
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var handler = new DeleteItemCommandHandler(_itemRepo.Object, _storage.Object, _user.Object);
        await handler.Handle(new DeleteItemCommand(item.Id), default);

        _storage.Verify(s => s.DeleteAsync("p.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
