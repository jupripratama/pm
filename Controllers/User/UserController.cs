using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Services;
using FluentValidation;

namespace Pm.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        private readonly IValidator<UpdateUserDto> _updateUserValidator;

        public UserController(
            IUserService userService, 
            ILogger<UserController> logger,
            IValidator<CreateUserDto> createUserValidator,
            IValidator<UpdateUserDto> updateUserValidator)
        {
            _userService = userService;
            _logger = logger;
            _createUserValidator = createUserValidator;
            _updateUserValidator = updateUserValidator;
        }

        /// <summary>
        /// Endpoint untuk mendapatkan daftar semua user dengan filter, sorting, dan pagination
        /// </summary>
        /// <param name="dto">Query parameters</param>
        /// <returns>Daftar user dalam format ter-paginasi</returns>
        [Authorize(Policy = "CanViewUsers")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryDto dto)
        {
            var users = await _userService.GetUsersAsync(dto);
            HttpContext.Items["message"] = "Daftar user berhasil dimuat";
            return Ok(users);
        }

        /// <summary>
        /// Endpoint untuk mendapatkan user berdasarkan ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Detail user</returns>
        [Authorize(Policy = "CanViewDetailUsers")]
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserById([FromRoute] int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "User tidak ditemukan" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "User berhasil dimuat";
            return Ok(user);
        }

        /// <summary>
        /// Endpoint untuk membuat user baru
        /// </summary>
        /// <param name="dto">Data user baru</param>
        /// <returns>User yang telah dibuat</returns>
        [Authorize(Policy = "CanCreateUsers")]
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            // Validasi menggunakan FluentValidation
            var validationResult = await _createUserValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateUser validation failed: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _userService.CreateUserAsync(dto);
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
                    message = "User berhasil dibuat",
                    data = user,
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
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
        /// Endpoint untuk memperbarui user yang sudah ada
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="dto">Data user yang akan diupdate</param>
        /// <returns>User yang telah diupdate</returns>
        [Authorize(Policy = "CanUpdateUsers")]
        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserDto dto)
        {
            // Cek apakah user ada
            var existingUser = await _userService.GetUserEntityByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "User tidak ditemukan" },
                    meta = (object?)null
                });
            }

            // Validasi menggunakan FluentValidation
            var validationResult = await _updateUserValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    statusCode = 400,
                    message = "Bad Request",
                    data = errors,
                    meta = (object?)null
                });
            }

            // Cek apakah ada perubahan data
            bool isSame =
                (dto.FullName == null || dto.FullName == existingUser.FullName) &&
                (dto.Username == null || dto.Username == existingUser.Username) &&
                (dto.Email == null || dto.Email == existingUser.Email) &&
                (dto.RoleId == null || dto.RoleId == existingUser.RoleId) &&
                (dto.IsActive == null || dto.IsActive == existingUser.IsActive);

            if (isSame)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = "Tidak ada perubahan data",
                    data = new { message = "Tidak ada field yang berubah" },
                    meta = (object?)null
                });
            }

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, dto);
                if (!updated)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "User tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                var user = await _userService.GetUserByIdAsync(userId);
                HttpContext.Items["message"] = "User berhasil diperbarui";
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
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
        /// Endpoint untuk menghapus user berdasarkan UserId
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Status penghapusan</returns>
        [Authorize(Policy = "CanDeleteUsers")]
        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var deleted = await _userService.DeleteUserAsync(userId);
            if (!deleted)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "User tidak ditemukan" },
                    meta = (object?)null
                });
            }

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = new { message = "User berhasil dihapus" },
                meta = (object?)null
            });
        }
    }
}