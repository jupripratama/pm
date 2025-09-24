using Pm.DTOs;
using Pm.DTOs.Auth;

namespace Pm.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task UpdateLastLoginAsync(int userId);
    }
}