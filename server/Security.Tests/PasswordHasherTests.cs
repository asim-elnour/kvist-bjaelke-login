using Security.Services.Security;

namespace Security.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void SamePassword_GivesTwoDifferentHashes()
    {
        var hasher = new Argon2PasswordHasher();

        var first = hasher.HashAndSaltPassword("Secret123");
        var second = hasher.HashAndSaltPassword("Secret123");

        Assert.NotEqual(first, second);
    }
}
