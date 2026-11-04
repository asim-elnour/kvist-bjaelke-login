using DataAccess.Entities;

namespace Security.Services.Authorization;

public interface ITokenService
{
    string GenerateToken(User user);
}