// Controllers/AuthController.cs - FIXED REGISTER ENDPOINT
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Pm.DTOs.Auth;
using Pm.Services;
using Pm.DTOs;
using FluentValidation;

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

                // ✅ VALIDASI MENGGUNAKAN FLUENTVALIDATION
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

                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Validation failed",
                        data = errors,
                        meta = (object?)null
                    });
                }

                // ✅ CEK USERNAME DUPLICATE
                if (await _userService.IsUsernameExistsAsync(dto.Username))
                {
                    _logger.LogWarning("❌ Username already exists: {Username}", dto.Username);
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Username sudah digunakan",
                        data = new { username = new[] { "Username sudah digunakan" } },
                        meta = (object?)null
                    });
                }

                // ✅ CEK EMAIL DUPLICATE
                if (await _userService.IsEmailExistsAsync(dto.Email))
                {
                    _logger.LogWarning("❌ Email already exists: {Email}", dto.Email);
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Email sudah digunakan",
                        data = new { email = new[] { "Email sudah digunakan" } },
                        meta = (object?)null
                    });
                }

                // ✅ CREATE USER
                var createUserDto = new CreateUserDto
                {
                    Username = dto.Username.Trim(),
                    Password = dto.Password,
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email.Trim(),
                    RoleId = 3 // Default role: User
                };

                var user = await _userService.CreateUserAsync(createUserDto);
                if (user == null)
                {
                    _logger.LogError("❌ Failed to create user");
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Gagal membuat user",
                        data = new { message = "Terjadi kesalahan saat membuat user" },
                        meta = (object?)null
                    });
                }

                _logger.LogInformation("✅ User registered successfully - ID: {UserId}, Username: {Username}",
                    user.UserId, user.Username);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    statusCode = StatusCodes.Status201Created,
                    message = "Registrasi berhasil. Akun Anda menunggu aktivasi dari Admin.",
                    data = user,
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during registration");
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message,
                    data = new { },
                    meta = (object?)null
                });
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
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(dto);
            if (result == null)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = "Username atau password salah, atau akun belum diaktivasi",
                    data = new { },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Login berhasil";
            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "Login berhasil",
                data = result,
                meta = (object?)null
            });
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
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = "Data tidak valid",
                    data = new { },
                    meta = (object?)null
                });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _authService.ChangePasswordAsync(userId, dto);
                if (!result)
                {
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Password lama tidak valid",
                        data = new { },
                        meta = (object?)null
                    });
                }

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Password berhasil diubah",
                    data = new { },
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message,
                    data = new { },
                    meta = (object?)null
                });
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
                return Unauthorized();
            }

            var userDto = await _userService.GetUserByIdAsync(userId);
            if (userDto == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "User tidak ditemukan",
                    data = new { },
                    meta = (object?)null
                });
            }

            return Ok(new
            {
                statusCode = 200,
                message = "Profile berhasil dimuat",
                data = userDto,
                meta = (object?)null
            });
        }

        /// <summary>
        /// Logout endpoint (untuk client-side token cleanup)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "Logout berhasil",
                data = new { },
                meta = (object?)null
            });
        }
    }
}