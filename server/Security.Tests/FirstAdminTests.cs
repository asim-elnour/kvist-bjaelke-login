using Security.Services;
using Security.Services.Security;

namespace Security.Tests;

public class FirstAdminTests
{
    [Fact]
    public void AdminExists_CreatesNoSecondOne()
    {
        var repository = TestDatabase.CreateRepository();
        var service = new UserService(repository, new Argon2PasswordHasher());

        service.CreateFirstAdmin("boss", "Secret123");
        service.CreateFirstAdmin("other", "Secret456");

        Assert.Single(repository.GetAll());
    }
}
