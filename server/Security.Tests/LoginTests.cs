using DataAccess.Entities;
using Security.Dtos;
using Security.Services;
using Security.Services.Security;

namespace Security.Tests;

public class LoginTests
{
    private static UserService CreateServiceWithAlice()
    {
        var hasher = new Argon2PasswordHasher();
        var repository = UserRepositoryTests.CreateRepository();
        repository.Create(new User
        {
            Username = "alice",
            PasswordHash = hasher.HashAndSaltPassword("Secret123"),
            Role = "user"
        });
        return new UserService(repository, hasher);
    }

    [Fact]
    public void RightPassword_ReturnsTheUser()
    {
        var user = CreateServiceWithAlice().TryLogin(new LoginRequest("alice", "Secret123"));

        Assert.Equal("alice", user?.Username);
    }

    [Fact]
    public void WrongPassword_ReturnsNull()
    {
        Assert.Null(CreateServiceWithAlice().TryLogin(new LoginRequest("alice", "Wrong123")));
    }

    [Fact]
    public void EmptyPassword_ReturnsNull()
    {
        Assert.Null(CreateServiceWithAlice().TryLogin(new LoginRequest("alice", "")));
    }

    [Fact]
    public void UnknownUser_ReturnsNull()
    {
        Assert.Null(CreateServiceWithAlice().TryLogin(new LoginRequest("nobody", "Secret123")));
    }
}