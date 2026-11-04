using Security.Services.Security;

namespace Security.Tests;

public class PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void SamePassword_GivesTwoDifferentHashes()
    {
        var first = _hasher.HashAndSaltPassword("Secret123");
        var second = _hasher.HashAndSaltPassword("Secret123");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_DoesNotContainThePassword()
    {
        var hash = _hasher.HashAndSaltPassword("Secret123");

        Assert.DoesNotContain("Secret123", hash);
    }

    [Fact]
    public void RightPassword_Passes()
    {
        var hash = _hasher.HashAndSaltPassword("Secret123");

        Assert.True(_hasher.VerifyPassword("Secret123", hash));
    }

    [Fact]
    public void WrongPassword_Fails()
    {
        var hash = _hasher.HashAndSaltPassword("Secret123");

        Assert.False(_hasher.VerifyPassword("Wrong123", hash));
    }
}