using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Pm.DTOs.Auth;
using Pm.Services;
using Pm.DTOs;
using FluentValidation;
using Pm.Helper;

namespace Pm.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly IValidator<RegisterDto> _registerValidator;

        public AuthController(
            IAuthService authService,
            IUserService userService,
            ILogger<AuthController> logger,
            IValidator<RegisterDto> registerValidator)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
            _registerValidator = registerValidator;
        }

        /// <summary>
        /// Register user baru (IsActive = false, perlu aktivasi dari Super Admin)
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("📝 Register attempt - Username: {Username}, Email: {Email}",
                    dto.Username, dto.Email);

                var validationResult = await _registerValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("❌ Validation failed for registration");

                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    return BadRequest(new { data = errors });
                }

                if (await _userService.IsUsernameExistsAsync(dto.Username))
                {
                    _logger.LogWarning("❌ Username already exists: {Username}", dto.Username);
                    return ApiResponse.BadRequest("username", "Username sudah digunakan");
                }

                if (await _userService.IsEmailExistsAsync(dto.Email))
                {
                    _logger.LogWarning("❌ Email already exists: {Email}", dto.Email);
                    return ApiResponse.BadRequest("email", "Email sudah digunakan");
                }

                var createUserDto = new CreateUserDto
                {
                    Username = dto.Username.Trim(),
                    Password = dto.Password,
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email.Trim(),
                    RoleId = 3
                };

                var user = await _userService.CreateUserAsync(createUserDto);
                if (user == null)
                {
                    _logger.LogError("❌ Failed to create user");
                    return ApiResponse.BadRequest("message", "Gagal membuat user");
                }

                _logger.LogInformation("✅ User registered successfully - ID: {UserId}, Username: {Username}",
                    user.UserId, user.Username);

                HttpContext.Items["message"] = "Registrasi berhasil. Akun Anda menunggu aktivasi dari Admin.";
                return ApiResponse.Created(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during registration");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        /// <summary>
        /// Login endpoint untuk autentikasi user
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            var result = await _authService.LoginAsync(dto);
            if (result == null)
            {
                return ApiResponse.Unauthorized();
            }

            HttpContext.Items["message"] = "Login berhasil";
            return ApiResponse.Success(result);
        }

        /// <summary>
        /// Change password endpoint untuk user yang sudah login
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return ApiResponse.Unauthorized();
            }

            try
            {
                var result = await _authService.ChangePasswordAsync(userId, dto);
                if (!result)
                {
                    return ApiResponse.BadRequest("message", "Password lama tidak valid");
                }

                HttpContext.Items["message"] = "Password berhasil diubah";
                return ApiResponse.Success(new { });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return ApiResponse.Unauthorized();
            }

            var userDto = await _userService.GetUserByIdAsync(userId);
            if (userDto == null)
            {
                return ApiResponse.NotFound("User tidak ditemukan");
            }

            HttpContext.Items["message"] = "Profile berhasil dimuat";
            return ApiResponse.Success(userDto);
        }

        /// <summary>
        /// Logout endpoint (untuk client-side token cleanup)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            HttpContext.Items["message"] = "Logout berhasil";
            return ApiResponse.Success(new { });
        }
    }
}