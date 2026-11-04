using DataAccess.Repositories;
using Security.Services;
using Security.Services.Security;

namespace Security.Tests;

public class FirstAdminTests
{
    private readonly UserRepository _repository = UserRepositoryTests.CreateRepository();
    private readonly UserService _service;

    public FirstAdminTests()
    {
        _service = new UserService(_repository, new Argon2PasswordHasher());
    }

    [Fact]
    public void NoAdminYet_CreatesTheAdmin()
    {
        _service.CreateFirstAdmin("boss", "Secret123");

        Assert.Equal("admin", _repository.GetByUsername("boss")?.Role);
    }

    [Fact]
    public void Password_IsStoredHashed()
    {
        _service.CreateFirstAdmin("boss", "Secret123");

        Assert.DoesNotContain("Secret123", _repository.GetByUsername("boss")!.PasswordHash);
    }

    [Fact]
    public void AdminExists_CreatesNoSecondOne()
    {
        _service.CreateFirstAdmin("boss", "Secret123");
        _service.CreateFirstAdmin("other", "Secret456");

        Assert.Single(_repository.GetAll());
    }

    [Fact]
    public void NoSettings_CreatesNobody()
    {
        _service.CreateFirstAdmin(null, null);

        Assert.Empty(_repository.GetAll());
    }
}