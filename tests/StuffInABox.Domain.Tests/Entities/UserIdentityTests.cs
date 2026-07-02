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
    public void CreateOAuth_StoresEmailWhenProvided()
    {
        var id = UserIdentity.CreateOAuth("google", "sub-123", "  User@Example.com ");

        Assert.Equal("User@Example.com", id.Email);
    }

    [Fact]
    public void CreateOAuth_NoEmail_LeavesEmailNull()
    {
        var id = UserIdentity.CreateOAuth("apple", "sub-123", "   ");

        Assert.Null(id.Email);
    }

    [Fact]
    public void SetEmailFromProvider_BackfillsWhenMissing()
    {
        var id = UserIdentity.CreateOAuth("google", "sub-123");

        id.SetEmailFromProvider("user@example.com");

        Assert.Equal("user@example.com", id.Email);
    }

    [Fact]
    public void SetEmailFromProvider_DoesNotOverwriteExisting()
    {
        var id = UserIdentity.CreateOAuth("google", "sub-123", "first@example.com");

        id.SetEmailFromProvider("second@example.com");

        Assert.Equal("first@example.com", id.Email);
    }

    [Fact]
    public void SetEmailFromProvider_IgnoresEmptyIncoming()
    {
        var id = UserIdentity.CreateOAuth("google", "sub-123");

        id.SetEmailFromProvider("  ");

        Assert.Null(id.Email);
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

    [Fact]
    public void CreateOAuth_IsItsOwnPerson()
    {
        var id = UserIdentity.CreateOAuth("google", "sub-123");

        Assert.Equal(id.InternalUserId, id.UserId);
        Assert.Equal(id.UserId, id.GetUserId().Value);
    }

    [Fact]
    public void CreateEmail_IsItsOwnPerson()
    {
        var id = UserIdentity.CreateEmail("hashed", "pwhash", "user@example.com");

        Assert.Equal(id.InternalUserId, id.UserId);
    }

    [Fact]
    public void CreateOAuthLinked_SharesThePersonButKeepsItsOwnLoginRow()
    {
        var person = Guid.NewGuid();

        var linked = UserIdentity.CreateOAuthLinked("google", "sub-123", "user@example.com", person);

        Assert.Equal(person, linked.UserId);            // same person → same data
        Assert.Equal(person, linked.GetUserId().Value);
        Assert.NotEqual(person, linked.InternalUserId); // but its own login-method key
        Assert.True(linked.IsEmailVerified);
    }

    [Fact]
    public void CreateOAuthLinked_EmptyPerson_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => UserIdentity.CreateOAuthLinked("google", "sub-123", "e@x.com", Guid.Empty));
    }
}
