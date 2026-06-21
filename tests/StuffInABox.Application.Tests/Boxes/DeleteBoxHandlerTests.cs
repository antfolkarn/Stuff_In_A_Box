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
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public DeleteBoxHandlerTests()
    {
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);
    }

    [Fact]
    public async Task Handle_DeletesBox_AndCascadesItemsWithPhotos()
    {
        var box = Box.Create(new BoxNumber(2), _spaceId, _userId, "Verktyg");
        _boxRepo.Setup(r => r.GetByNumberAsync(It.IsAny<BoxNumber>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(box);

        var withPhoto = Item.Create(box.Number, _userId, "Hammare");
        withPhoto.SetPhoto("photo123.jpg");
        var noPhoto = Item.Create(box.Number, _userId, "Skruvar");
        _itemRepo.Setup(r => r.GetByBoxAsync(box.Number, _userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { withPhoto, noPhoto });

        var handler = new DeleteBoxCommandHandler(_boxRepo.Object, _itemRepo.Object, _storage.Object, _access.Object);
        await handler.Handle(new DeleteBoxCommand(2, _spaceId), default);

        _storage.Verify(s => s.DeleteAsync("photo123.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(withPhoto.Id, It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(noPhoto.Id, It.IsAny<CancellationToken>()), Times.Once);
        _boxRepo.Verify(r => r.DeleteAsync(box.Number, _userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
