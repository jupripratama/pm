using Pm.Models;

namespace Pm.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user, List<string> permissions);
        int GetTokenExpirationTime();
    }
}