using Moq;
using StuffInABox.Application.Boxes.Commands.DeleteBox;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Boxes;

public class DeleteBoxHandlerTests
{
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly UserId _userId = new(Guid.NewGuid());

    public DeleteBoxHandlerTests() => _user.Setup(u => u.UserId).Returns(_userId);

    [Fact]
    public async Task Handle_DeletesBox_AndCascadesItemsWithPhotos()
    {
        var box = Box.Create(new BoxNumber(2), Guid.NewGuid(), _userId, "Verktyg");
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(box);

        var withPhoto = Item.Create(box.Number, _userId, "Hammare");
        withPhoto.SetPhoto("photo123.jpg");
        var noPhoto = Item.Create(box.Number, _userId, "Skruvar");
        _itemRepo.Setup(r => r.GetByBoxAsync(box.Number, _userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { withPhoto, noPhoto });

        var handler = new DeleteBoxCommandHandler(_boxRepo.Object, _itemRepo.Object, _storage.Object, _user.Object);
        await handler.Handle(new DeleteBoxCommand(2), default);

        _storage.Verify(s => s.DeleteAsync("photo123.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(withPhoto.Id, It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(noPhoto.Id, It.IsAny<CancellationToken>()), Times.Once);
        _boxRepo.Verify(r => r.DeleteAsync(box.Number, _userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
