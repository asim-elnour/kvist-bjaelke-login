using DataAccess.Entities;
using DataAccess.Repositories;
using Security.Dtos;
using Security.Services.Security;

namespace Security.Services;

public class UserService(IUserRepository users, IPasswordHasher passwordHasher) : IUserService
{
    public User? TryLogin(LoginRequest request)
    {
        var user = users.GetByUsername(request.Username);
        if (user == null || string.IsNullOrEmpty(request.Password)) return null;

        return passwordHasher.VerifyPassword(request.Password, user.PasswordHash) ? user : null;
    }

    public void CreateFirstAdmin(string? username, string? password)
    {
        // Once an admin exists, the settings are not used again.
        if (users.GetAll().Any(user => user.Role == "admin")) return;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return;

        users.Create(new User
        {
            Username = username,
            PasswordHash = passwordHasher.HashAndSaltPassword(password),
            Role = "admin"
        });
    }
}