using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public interface IUserService
    {
        Task<PagedResultDto<UserDto>> GetUsersAsync(UserQueryDto query);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetUserEntityByIdAsync(int id);
        Task<bool> RoleExistsAsync(int roleId);
    }
}
