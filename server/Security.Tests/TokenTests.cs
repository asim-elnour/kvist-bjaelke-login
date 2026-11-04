using System.IdentityModel.Tokens.Jwt;
using DataAccess.Entities;
using Microsoft.Extensions.Configuration;
using Security.Services.Authorization;

namespace Security.Tests;

public class TokenTests
{
    private static JwtSecurityToken CreateToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSecret"] = "a-secret-that-is-only-used-in-these-tests"
            })
            .Build();
        var user = new User { Id = 7, Username = "alice", PasswordHash = "hash", Role = "admin" };

        var token = new JwtTokenService(configuration).GenerateToken(user);

        return new JwtSecurityTokenHandler().ReadJwtToken(token);
    }

    [Fact]
    public void Token_HoldsTheIdAndRole()
    {
        var token = CreateToken();

        Assert.Contains(token.Claims, c => c.Value == "7");
        Assert.Contains(token.Claims, c => c.Value == "admin");
    }

    [Fact]
    public void Token_ExpiresWithinAnHour()
    {
        var expires = CreateToken().ValidTo;

        Assert.True(expires > DateTime.UtcNow);
        Assert.True(expires <= DateTime.UtcNow.AddHours(1));
    }
}