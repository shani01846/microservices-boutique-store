namespace ECommerce.Application.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string email, string role);
    bool ValidateToken(string token);
    int GetUserIdFromToken(string token);
}