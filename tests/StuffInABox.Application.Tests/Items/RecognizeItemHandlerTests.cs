using Moq;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Application.Items.Commands.RecognizeItem;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Tests.Items;

public class RecognizeItemHandlerTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IBoxRepository> _boxRepo = new();
    private readonly Mock<ISpaceAccessService> _access = new();
    private readonly Mock<IEntitlementService> _entitlements = new();
    private readonly Mock<IImageRecognitionQueue> _queue = new();
    private readonly UserId _userId = new(Guid.NewGuid());
    private readonly Guid _spaceId = Guid.NewGuid();

    private RecognizeItemCommandHandler Handler() =>
        new(_itemRepo.Object, _boxRepo.Object, _access.Object, _entitlements.Object, _queue.Object);

    private Item SetupPhotoItem(bool withPhoto = true)
    {
        var item = Item.CreateFromPhoto(new BoxNumber(1), _userId);
        if (withPhoto)
        {
            item.SetPhoto("key.jpg", 1000);
            item.MarkAiSkipped(); // a photo item that hasn't been AI-analyzed
        }
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _boxRepo.Setup(r => r.GetByNumberAsync(item.BoxNumber, item.OwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Box.Create(item.BoxNumber, _spaceId, item.OwnerId, "Box"));
        _access.Setup(a => a.RequireSpaceAsync(_spaceId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(_userId);
        return item;
    }

    [Fact]
    public async Task Handle_WithCredit_MarksPendingAndEnqueues()
    {
        var item = SetupPhotoItem();

        await Handler().Handle(new RecognizeItemCommand(item.Id), default);

        Assert.Equal(ItemEnrichmentStatus.Pending, item.EnrichmentStatus);
        _queue.Verify(q => q.EnqueueRecognition(item.Id), Times.Once);
        _entitlements.Verify(e => e.EnsureAiCreditAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyAnalyzed_IsNoOp()
    {
        var item = SetupPhotoItem();
        item.ApplyRecognition("Skruvdragare", ["verktyg"]); // now Completed

        await Handler().Handle(new RecognizeItemCommand(item.Id), default);

        _queue.Verify(q => q.EnqueueRecognition(It.IsAny<Guid>()), Times.Never);
        _entitlements.Verify(e => e.EnsureAiCreditAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoPhoto_Throws()
    {
        var item = SetupPhotoItem(withPhoto: false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => Handler().Handle(new RecognizeItemCommand(item.Id), default));
        _queue.Verify(q => q.EnqueueRecognition(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OverAiQuota_Throws_AndDoesNotEnqueue()
    {
        var item = SetupPhotoItem();
        _entitlements.Setup(e => e.EnsureAiCreditAsync(_userId, It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new QuotaExceededException("ai", 5, "free"));

        await Assert.ThrowsAsync<QuotaExceededException>(
            () => Handler().Handle(new RecognizeItemCommand(item.Id), default));
        _queue.Verify(q => q.EnqueueRecognition(It.IsAny<Guid>()), Times.Never);
    }
}
