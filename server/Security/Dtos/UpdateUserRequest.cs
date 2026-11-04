using System.ComponentModel.DataAnnotations;

namespace Security.Dtos;

// Password is optional: leave it empty to keep the old one.
public record UpdateUserRequest(
    [Required] string Username,
    string? Password,
    [Required, AllowedValues("user", "superuser", "admin")] string Role);