using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Interfaces;

public interface ICurrentUserService
{
    UserId UserId { get; }
    bool IsAuthenticated { get; }
}
