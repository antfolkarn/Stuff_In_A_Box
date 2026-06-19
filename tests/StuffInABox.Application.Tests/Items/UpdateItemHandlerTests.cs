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
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public UpdateItemHandlerTests() => _user.Setup(u => u.UserId).Returns(_userId);

    [Fact]
    public async Task Handle_UpdatesNameAndTags()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Gammalt");
        item.ReplaceTags(new[] { "gammal" });
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _user.Object);
        await handler.Handle(new UpdateItemCommand(item.Id, "Nytt", new[] { "ny", "tagg" }), default);

        Assert.Equal("Nytt", item.Name);
        Assert.Equal(new[] { "ny", "tagg" }, item.Tags);
    }

    [Fact]
    public async Task Handle_OtherUsersItem_Throws()
    {
        var item = Item.Create(new BoxNumber(1), new UserId(Guid.NewGuid()), "Annans");
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _user.Object);
        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => handler.Handle(new UpdateItemCommand(item.Id, "x", null), default));
    }
}
