namespace UserService.DTOs;

public record RegisterDto(string Email, string Password, string FirstName, string LastName);
public record LoginDto(string Email, string Password);
public record UserDto(int Id, string Email, string FirstName, string LastName, string Role);
public record AuthResponseDto(string Token, UserDto User);
