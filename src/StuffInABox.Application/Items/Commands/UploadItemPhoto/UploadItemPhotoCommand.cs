using MediatR;

namespace StuffInABox.Application.Items.Commands.UploadItemPhoto;

public sealed record UploadItemPhotoCommand(Guid ItemId, byte[] Content, string FileName)
    : IRequest<UploadItemPhotoResult>;

public sealed record UploadItemPhotoResult(string PhotoUrl);
