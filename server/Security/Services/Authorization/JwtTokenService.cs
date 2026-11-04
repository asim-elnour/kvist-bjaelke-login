using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataAccess.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Security.Services.Authorization;

public class JwtTokenService(IConfiguration configuration) : ITokenService
{
    private const string Issuer = "security-api";
    private const string Audience = "security-api";
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(
        configuration["JwtSecret"] ?? throw new InvalidOperationException("JwtSecret is missing.")));

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}