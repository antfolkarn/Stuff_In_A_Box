using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Web.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public UserId UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claim is null || !Guid.TryParse(claim, out var guid))
                throw new UnauthorizedAccessException("User is not authenticated.");
            return new UserId(guid);
        }
    }
}
