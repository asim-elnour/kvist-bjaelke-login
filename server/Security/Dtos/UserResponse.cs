namespace Security.Dtos;

// What the API sends back about a user. It has no password hash on purpose.
public record UserResponse(int Id, string Username, string Role);