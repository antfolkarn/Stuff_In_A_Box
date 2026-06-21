using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Items.Commands.UploadItemPhoto;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Items;

public class UploadItemPhotoHandlerTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<IImageProcessor> _processor = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    public UploadItemPhotoHandlerTests()
    {
        _processor.Setup(p => p.ProcessAndStripMetadata(It.IsAny<byte[]>()))
                  .Returns(new ProcessedImage(new byte[] { 1, 2, 3 }, ".jpg", "image/jpeg"));
        _storage.Setup(s => s.StoreAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("stored-key.jpg");
        _storage.Setup(s => s.GetUrl("stored-key.jpg")).Returns("/uploads/stored-key.jpg");
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);
    }

    private UploadItemPhotoCommandHandler CreateHandler() =>
        new(_itemRepo.Object, _boxRepo.Object, _processor.Object, _storage.Object, _access.Object);

    private void SetupItemAndBox(Item item)
    {
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _boxRepo.Setup(r => r.GetByNumberAsync(item.BoxNumber, item.OwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(new BoxNumber(item.BoxNumber.Value), _spaceId, item.OwnerId, "Box"));
    }

    [Fact]
    public async Task Handle_ValidImage_StoresAndSetsPhoto()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Jacka");
        SetupItemAndBox(item);

        var result = await CreateHandler().Handle(
            new UploadItemPhotoCommand(item.Id, new byte[] { 0xFF, 0xD8, 0xFF }, "x.jpg"), default);

        Assert.Equal("/uploads/stored-key.jpg", result.PhotoUrl);
        Assert.Equal("stored-key.jpg", item.PhotoStorageKey);
        _itemRepo.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReplacesOldPhoto_DeletesPrevious()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Jacka");
        item.SetPhoto("old-key.jpg");
        SetupItemAndBox(item);

        await CreateHandler().Handle(
            new UploadItemPhotoCommand(item.Id, new byte[] { 0xFF, 0xD8, 0xFF }, "x.jpg"), default);

        _storage.Verify(s => s.DeleteAsync("old-key.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TooLarge_Throws()
    {
        var item = Item.Create(new BoxNumber(1), _userId, "Jacka");
        SetupItemAndBox(item);
        var big = new byte[UploadItemPhotoCommandHandler.MaxBytes + 1];

        await Assert.ThrowsAsync<InvalidImageException>(
            () => CreateHandler().Handle(new UploadItemPhotoCommand(item.Id, big, "x.jpg"), default));
    }

    [Fact]
    public async Task Handle_ItemNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _itemRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Item?)null);

        await Assert.ThrowsAsync<Domain.Exceptions.NotFoundException>(
            () => CreateHandler().Handle(new UploadItemPhotoCommand(id, new byte[] { 0xFF, 0xD8, 0xFF }, "x.jpg"), default));
    }
}
