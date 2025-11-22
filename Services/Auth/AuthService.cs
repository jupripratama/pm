using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Auth;
using Pm.Models;

namespace Pm.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("🔐 Login attempt for username: {Username}", dto.Username);

            var user = await _context.Users
                .AsTracking()
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("❌ Login failed: User {Username} not found or inactive", dto.Username);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("❌ Login failed: Invalid password for user {Username}", dto.Username);
                return null;
            }

            // Get user permissions
            var permissions = user.Role?.RolePermissions
                .Select(rp => rp.Permission.PermissionName)
                .ToList() ?? new List<string>();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user, permissions);
            var expiresIn = _jwtService.GetTokenExpirationTime();

            // ✅ GUNAKAN DateTimeOffset untuk TRUE UTC
            var trueUtcTime = DateTimeOffset.UtcNow.DateTime;
            
            _logger.LogInformation("🕐 Server Local Time: {Local}", DateTime.Now);
            _logger.LogInformation("🕐 TRUE UTC Time: {Utc}", trueUtcTime);
            _logger.LogInformation("🕐 Server Timezone: {Tz}", TimeZoneInfo.Local.Id);
            
            user.LastLogin = trueUtcTime;
            user.UpdatedAt = trueUtcTime;
            
            _context.Entry(user).State = EntityState.Modified;
            
            try
            {
                var rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("✅ LastLogin saved to DB - UTC: {LastLogin} (Rows: {Rows})", 
                    user.LastLogin, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update LastLogin");
            }

            return new LoginResponseDto
            {
                Token = token,
                ExpiresIn = expiresIn,
                User = new DTOs.UserDto
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
                    CreatedAt = user.CreatedAt,
                    Permissions = permissions
                },
                Permissions = permissions
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            _logger.LogInformation("🔐 Change password attempt for user {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("❌ Change password failed: User {UserId} not found", userId);
                return false;
            }

            // Verify current password
            var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            _logger.LogInformation("🔐 Current password validation result: {IsValid}", isCurrentPasswordValid);

            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("❌ Change password failed: Invalid current password for user {UserId}", userId);
                return false;
            }

            // Validate new password strength
            _logger.LogInformation("🔐 Validating new password strength...");
            var isStrong = IsStrongPassword(dto.NewPassword);
            _logger.LogInformation("🔐 New password strength validation result: {IsStrong}", isStrong);

            if (!isStrong)
            {
                _logger.LogWarning("❌ Change password failed: New password doesn't meet strength requirements for user {UserId}", userId);
                throw new Exception("Password baru harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol.");
            }

            // Hash new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // ⚠️ PERBAIKAN: Explicitly update the entity state
            user.PasswordHash = newPasswordHash;
            user.UpdatedAt = DateTime.UtcNow;

            // ⚠️ PERBAIKAN: Mark entity as modified
            _context.Entry(user).State = EntityState.Modified;

            try
            {
                var rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Password changed successfully for user {UserId}. Rows affected: {RowsAffected}", userId, rowsAffected);

                // ⚠️ PERBAIKAN: Return true even if rowsAffected is 0, as long as no exception
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Database error while changing password for user {UserId}", userId);
                throw new Exception("Gagal menyimpan password baru ke database.");
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
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