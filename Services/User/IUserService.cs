using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public interface IUserService
    {
        Task<PagedResultDto<UserDto>> GetUsersAsync(UserQueryDto queryDto);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<User?> GetUserEntityByIdAsync(int userId);
        Task<UserDto?> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null);
        Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null);
        Task<bool> UpdateUserPhotoAsync(int userId, string? photoUrl);
        Task<bool> RoleExistsAsync(int roleId);
    }
}
