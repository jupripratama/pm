using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;
using Pm.Helper;

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

        private static UserDto ToDto(User user) => new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            RoleId = user.RoleId,
            RoleName = user.Role?.RoleName,
            LastLoginText = user.LastLogin.HasValue
                ? TimeHelper.GetRelativeTime(user.LastLogin.Value)
                : "Belum login",
            LastLogin = user.LastLogin,
            CreatedAt = user.CreatedAt
        };

        public async Task<PagedResultDto<UserDto>> GetUsersAsync(UserQueryDto dto)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(dto.Search))
            {
                query = query.Where(u =>
                    u.Username.Contains(dto.Search) ||
                    u.FullName.Contains(dto.Search) ||
                    (u.Email != null && u.Email.Contains(dto.Search)) ||
                    (u.Role != null && u.Role.RoleName.Contains(dto.Search)));
            }

            if (dto.RoleId.HasValue)
            {
                query = query.Where(u => u.RoleId == dto.RoleId.Value);
            }

            if (dto.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == dto.IsActive.Value);
            }

            // Apply sorting
            var sortDir = dto.SortDir?.ToLower() ?? "desc";
            if (!string.IsNullOrWhiteSpace(dto.SortBy))
            {
                query = dto.SortBy.ToLower() switch
                {
                    "fullname" => sortDir == "desc"
                        ? query.OrderByDescending(u => u.FullName)
                        : query.OrderBy(u => u.FullName),
                    "username" => sortDir == "desc"
                        ? query.OrderByDescending(u => u.Username)
                        : query.OrderBy(u => u.Username),
                    "email" => sortDir == "desc"
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "rolename" => sortDir == "desc"
                        ? query.OrderByDescending(u => u.Role != null ? u.Role.RoleName : "")
                        : query.OrderBy(u => u.Role != null ? u.Role.RoleName : ""),
                    "createdat" => sortDir == "desc"
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderByDescending(u => u.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            var total = await query.CountAsync();

            var users = await query
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            return new PagedResultDto<UserDto>(
                users.Select(ToDto).ToList(),
                dto.Page,
                dto.PageSize,
                total
            );
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
            
            return user is null ? null : ToDto(user);
        }

        public async Task<User?> GetUserEntityByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<UserDto?> CreateUserAsync(CreateUserDto dto)
        {
            _logger.LogInformation("Attempting to create user with username: {Username}", dto.Username);

            // Check if username already exists
            if (await UsernameExistsAsync(dto.Username))
            {
                _logger.LogWarning("Username creation failed: Username {Username} already exists", dto.Username);
                return null;
            }

            // Check if email already exists
            if (!string.IsNullOrEmpty(dto.Email) && await EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Email creation failed: Email {Email} already exists", dto.Email);
                return null;
            }

            // Validate role
            if (dto.RoleId.HasValue && !await RoleExistsAsync(dto.RoleId.Value))
            {
                throw new ArgumentException("RoleId tidak valid.", nameof(dto.RoleId));
            }

            // Validate strong password
            if (!IsStrongPassword(dto.Password))
            {
                throw new Exception("Password harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol.");
            }

            try
            {
                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    FullName = dto.FullName,
                    Email = dto.Email,
                    IsActive = true,
                    RoleId = dto.RoleId ?? 3, // Default to User role
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();

                _logger.LogInformation("User created successfully: {Username} (ID: {UserId})", user.Username, user.UserId);
                return ToDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with username: {Username}", dto.Username);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto)
        {
            var user = await GetUserEntityByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Update failed: UserId {UserId} not found", userId);
                return false;
            }

            _logger.LogInformation("Start updating userId: {UserId}", userId);

            try
            {
                // Update Role
                if (dto.RoleId.HasValue && dto.RoleId != user.RoleId)
                {
                    if (!await RoleExistsAsync(dto.RoleId.Value))
                        throw new Exception("Role tidak valid.");

                    _logger.LogInformation("RoleId changed: {Old} -> {New}", user.RoleId, dto.RoleId);
                    user.RoleId = dto.RoleId.Value;
                }

                // Update Username
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
                {
                    if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.UserId != userId))
                        throw new Exception("Username sudah digunakan.");

                    _logger.LogInformation("Username changed: {Old} -> {New}", user.Username, dto.Username);
                    user.Username = dto.Username;
                }

                // Update Email
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId))
                        throw new Exception("Email sudah digunakan.");

                    _logger.LogInformation("Email changed: {Old} -> {New}", user.Email, dto.Email);
                    user.Email = dto.Email;
                }

                // Update FullName
                if (!string.IsNullOrWhiteSpace(dto.FullName) && dto.FullName != user.FullName)
                {
                    _logger.LogInformation("FullName changed: {Old} -> {New}", user.FullName, dto.FullName);
                    user.FullName = dto.FullName;
                }

                // Update IsActive
                if (dto.IsActive.HasValue && dto.IsActive != user.IsActive)
                {
                    _logger.LogInformation("IsActive changed: {Old} -> {New}", user.IsActive, dto.IsActive);
                    user.IsActive = dto.IsActive.Value;
                }

               

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Update completed for userId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Delete failed: UserId {UserId} not found.", id);
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User with UserId {UserId} successfully deleted.", id);
            return true;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> RoleExistsAsync(int roleId)
        {
            return await _context.Roles.AnyAsync(r => r.RoleId == roleId && r.IsActive);
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

