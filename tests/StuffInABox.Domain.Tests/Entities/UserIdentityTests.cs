using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Tests.Entities;

public class UserIdentityTests
{
    [Fact]
    public void CreateEmail_IsUnverified()
    {
        var id = UserIdentity.CreateEmail("hashed", "pwhash", "user@example.com");

        Assert.Null(id.EmailVerifiedAt);
        Assert.False(id.IsEmailVerified);
    }

    [Fact]
    public void CreateOAuth_IsVerified()
    {
        var id = UserIdentity.CreateOAuth("google", "sub-123");

        Assert.NotNull(id.EmailVerifiedAt);
        Assert.True(id.IsEmailVerified);
    }

    [Fact]
    public void MarkEmailVerified_VerifiesEmailAccount()
    {
        var id = UserIdentity.CreateEmail("hashed", "pwhash", "user@example.com");

        id.MarkEmailVerified();

        Assert.True(id.IsEmailVerified);
        Assert.NotNull(id.EmailVerifiedAt);
    }

    [Fact]
    public void MarkEmailVerified_IsIdempotent()
    {
        var id = UserIdentity.CreateEmail("hashed", "pwhash", "user@example.com");
        id.MarkEmailVerified();
        var first = id.EmailVerifiedAt;

        id.MarkEmailVerified();

        Assert.Equal(first, id.EmailVerifiedAt);
    }
}
