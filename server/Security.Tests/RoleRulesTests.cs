using Security.Services.Authorization;

namespace Security.Tests;

public class RoleRulesTests
{
    [Theory]
    [InlineData("user", "user", false)]
    [InlineData("user", "superuser", false)]
    [InlineData("user", "admin", false)]
    [InlineData("superuser", "user", true)]
    [InlineData("superuser", "superuser", false)]
    [InlineData("superuser", "admin", false)]
    [InlineData("admin", "user", true)]
    [InlineData("admin", "superuser", true)]
    [InlineData("admin", "admin", true)]
    [InlineData("unknown", "user", false)]
    public void CanChange_FollowsTheRoleTable(string actorRole, string targetRole, bool expected)
    {
        Assert.Equal(expected, RoleRules.CanChange(actorRole, targetRole));
    }
}