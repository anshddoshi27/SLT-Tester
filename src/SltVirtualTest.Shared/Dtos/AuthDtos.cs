namespace SltVirtualTest.Shared.Dtos;

public record SignUpRequest(
    string Username,
    string Password,
    string JobId,
    string Employer,
    string JobTitle);

public record LoginRequest(string Username, string Password);

public record UserDto(
    Guid Id,
    string Username,
    string JobId,
    string Employer,
    string JobTitle);

public record AuthResponse(bool Success, string? ErrorMessage, UserDto? User);
