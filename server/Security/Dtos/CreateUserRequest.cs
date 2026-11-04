using System.ComponentModel.DataAnnotations;

namespace Security.Dtos;

public record CreateUserRequest(
    [Required] string Username,
    [Required] string Password,
    [Required, AllowedValues("user", "superuser", "admin")] string Role);