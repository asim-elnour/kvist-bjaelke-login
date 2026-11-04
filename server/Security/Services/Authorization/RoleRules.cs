namespace Security.Services.Authorization;

public static class RoleRules
{
    // Decides if someone with actorRole may create, edit or delete someone with targetRole.
    public static bool CanChange(string actorRole, string targetRole)
    {
        if (actorRole == "admin") return true;
        if (actorRole == "superuser") return targetRole == "user";
        return false;
    }
}