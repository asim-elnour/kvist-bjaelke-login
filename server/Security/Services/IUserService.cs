using DataAccess.Entities;
using Security.Dtos;

namespace Security.Services;

public interface IUserService
{
    User? TryLogin(LoginRequest request);
    void CreateFirstAdmin(string? username, string? password);
}