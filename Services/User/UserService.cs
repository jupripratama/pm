using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResultDto<UserDto>> GetUsersAsync(UserQueryDto queryDto)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(queryDto.Search))
            {
                query = query.Where(u =>
                    u.Username.Contains(queryDto.Search) ||
                    u.FullName.Contains(queryDto.Search) ||
                    (u.Email != null && u.Email.Contains(queryDto.Search)));
            }

            // Filter by RoleId
            if (queryDto.RoleId.HasValue)
            {
                query = query.Where(u => u.RoleId == queryDto.RoleId.Value);
            }

            // Filter by IsActive
            if (queryDto.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == queryDto.IsActive.Value);
            }

            // Sorting
            query = ApplySorting(query, queryDto.SortBy, queryDto.SortDir);

            // Total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .Skip((queryDto.Page - 1) * queryDto.PageSize)
                .Take(queryDto.PageSize)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return new PagedResultDto<UserDto>(users, queryDto.Page, queryDto.PageSize, totalCount);
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var dto = MapToDto(user);

            // Add permissions from role
            if (user.RoleId > 0)
            {
                var permissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == user.RoleId)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission.PermissionName)
                    .ToListAsync();

                dto.Permissions = permissions;
            }

            return dto;
        }

        public async Task<User?> GetUserEntityByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<UserDto?> CreateUserAsync(CreateUserDto dto)
        {
            // Check if username exists
            if (await IsUsernameExistsAsync(dto.Username))
            {
                throw new Exception("Username sudah digunakan");
            }

            // Check if email exists
            if (!string.IsNullOrEmpty(dto.Email) && await IsEmailExistsAsync(dto.Email))
            {
                throw new Exception("Email sudah digunakan");
            }

            // Validate password strength
            if (!IsStrongPassword(dto.Password))
            {
                throw new Exception("Password harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol");
            }

            // Check if role exists
            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
            if (!roleExists)
            {
                throw new Exception("Role tidak ditemukan");
            }

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                Email = dto.Email,
                RoleId = dto.RoleId ?? 3, // Default to User role (RoleId = 3)
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {Username}", user.Username);

            return await GetUserByIdAsync(user.UserId);
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for update: {UserId}", userId);
                return false;
            }

            // Track changes untuk update
            _context.Entry(user).State = EntityState.Modified;

            // Check username uniqueness if changed
            if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
            {
                if (await IsUsernameExistsAsync(dto.Username, userId))
                {
                    throw new Exception("Username sudah digunakan");
                }
                user.Username = dto.Username;
            }

            // Check email uniqueness if changed
            if (dto.Email != null && dto.Email != user.Email)
            {
                if (!string.IsNullOrEmpty(dto.Email) && await IsEmailExistsAsync(dto.Email, userId))
                {
                    throw new Exception("Email sudah digunakan");
                }
                user.Email = dto.Email;
            }

            // Update other fields if provided
            if (!string.IsNullOrEmpty(dto.FullName))
            {
                user.FullName = dto.FullName;
            }

            if (dto.RoleId.HasValue)
            {
                // Check if role exists
                var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId.Value);
                if (!roleExists)
                {
                    throw new Exception("Role tidak ditemukan");
                }
                user.RoleId = dto.RoleId.Value;
            }

            if (dto.IsActive.HasValue)
            {
                user.IsActive = dto.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User updated successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for deletion: {UserId}", userId);
                return false;
            }

            // Prevent deleting the last super admin
            if (user.RoleId == 1) // Super Admin
            {
                var superAdminCount = await _context.Users.CountAsync(u => u.RoleId == 1);
                if (superAdminCount <= 1)
                {
                    throw new Exception("Tidak dapat menghapus Super Admin terakhir");
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted successfully: {UserId}", userId);
            return true;
        }

        public async Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Username == username);
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email == email);
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> UpdateUserPhotoAsync(int userId, string? photoUrl)
        {


            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for photo update: {UserId}", userId);
                return false;
            }



            user.PhotoUrl = photoUrl;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                // PASTIKAN INI ADA!
                _context.Users.Update(user); // atau _context.Entry(user).State = EntityState.Modified;
                var saved = await _context.SaveChangesAsync();

                if (saved > 0)
                {
                    _logger.LogInformation("User photo updated successfully: {UserId}, PhotoUrl: {PhotoUrl}", userId, photoUrl);
                    return true;
                }
                else
                {
                    _logger.LogError("SaveChanges returned 0 rows affected for user {UserId}", userId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user photo in database: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RoleExistsAsync(int roleId)
        {
            return await _context.Roles.AnyAsync(r => r.RoleId == roleId && r.IsActive);
        }

        private IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, string? sortDir)
        {
            var isDescending = sortDir?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "username" => isDescending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
                "fullname" => isDescending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                "email" => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "role" => isDescending ? query.OrderByDescending(u => u.Role!.RoleName) : query.OrderBy(u => u.Role!.RoleName),
                "isactive" => isDescending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                "createdat" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                "lastlogin" => isDescending ? query.OrderByDescending(u => u.LastLogin) : query.OrderBy(u => u.LastLogin),
                _ => query.OrderByDescending(u => u.CreatedAt) // Default sorting
            };
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhotoUrl = user.PhotoUrl,
                IsActive = user.IsActive,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName,
                LastLogin = user.LastLogin,
                LastLoginText = user.LastLogin?.ToString("dd MMM yyyy HH:mm") ?? "Belum pernah login",
                CreatedAt = user.CreatedAt,
                CreatedAtText = user.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                Permissions = new List<string>()
            };
        }

        private static bool IsStrongPassword(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}