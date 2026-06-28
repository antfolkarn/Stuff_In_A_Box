using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

/// <summary>
/// Test helper: marks a freshly-registered email account as verified directly in the DB.
/// Creating spaces/invites now requires a verified email, so tests that exercise those
/// flows verify their user the same way a real user would (just without the email round-trip).
/// </summary>
internal static class TestVerify
{
    public static void MarkVerified(WebApplicationFactory<Program> factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.UserIdentities.FirstOrDefault(u => u.Email == email);
        if (user is null) return;
        user.MarkEmailVerified();
        db.SaveChanges();
    }
}
