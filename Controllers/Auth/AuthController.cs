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
        /// <param name="dto">Register data</param>
        /// <returns>Created user</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Validasi menggunakan FluentValidation
            var validationResult = await _registerValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createUserDto = new CreateUserDto
                {
                    Username = dto.Username,
                    Password = dto.Password,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    RoleId = 3 // Default role: User
                };

                var user = await _userService.CreateUserAsync(createUserDto);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Gagal membuat user",
                        data = new { message = "Username atau email mungkin sudah digunakan" },
                        meta = (object?)null
                    });
                }

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
                _logger.LogError(ex, "Error during registration");
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
        /// <param name="dto">Login credentials</param>
        /// <returns>JWT token dan informasi user</returns>
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
            return Ok(result);
        }

        /// <summary>
        /// Change password endpoint untuk user yang sudah login
        /// </summary>
        /// <param name="dto">Password change data</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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
        /// <returns>Current user information</returns>
        [Authorize]
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = User.FindFirst("FullName")?.Value;
            var roleId = User.FindFirst("RoleId")?.Value;
            var roleName = User.FindFirst("RoleName")?.Value;
            var photoUrl = User.FindFirst("PhotoUrl")?.Value;
            var permissions = User.FindAll("Permission").Select(c => c.Value).ToList();

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var profile = new
            {
                userId = userId,
                username = username,
                email = email,
                fullName = fullName,
                photoUrl = photoUrl,
                roleId = int.TryParse(roleId, out var rId) ? rId : 0,
                roleName = roleName,
                permissions = permissions
            };

            HttpContext.Items["message"] = "Profile berhasil dimuat";
            return Ok(profile);
        }

        /// <summary>
        /// Logout endpoint (untuk client-side token cleanup)
        /// </summary>
        /// <returns>Success message</returns>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            // Dalam implementasi JWT, logout biasanya handled di client side
            // dengan menghapus token dari storage
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