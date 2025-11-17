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
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<UserController> _logger;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        private readonly IValidator<UpdateUserDto> _updateUserValidator;

        public UserController(
            IUserService userService,
            ICloudinaryService cloudinaryService,
            ILogger<UserController> logger,
            IValidator<CreateUserDto> createUserValidator,
            IValidator<UpdateUserDto> updateUserValidator)
        {
            _userService = userService;
            _cloudinaryService = cloudinaryService;
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
        /// ✅ ACTIVATE USER - Set isActive to TRUE
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Updated user</returns>
        [Authorize(Policy = "CanUpdateUsers")]
        [HttpPatch("{userId}/activate")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var user = await _userService.GetUserEntityByIdAsync(userId);
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

            var updateDto = new UpdateUserDto
            {
                IsActive = true
            };

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, updateDto);
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

                var updatedUser = await _userService.GetUserByIdAsync(userId);

                HttpContext.Items["message"] = "User berhasil diaktifkan";
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", userId);
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
        /// ✅ DEACTIVATE USER - Set isActive to FALSE
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Updated user</returns>
        [Authorize(Policy = "CanUpdateUsers")]
        [HttpPatch("{userId}/deactivate")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var user = await _userService.GetUserEntityByIdAsync(userId);
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

            var updateDto = new UpdateUserDto
            {
                IsActive = false
            };

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, updateDto);
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

                var updatedUser = await _userService.GetUserByIdAsync(userId);

                HttpContext.Items["message"] = "User berhasil dinonaktifkan";
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
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
        /// ✅ UPDATE ROLE - Change user's role
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="dto">New role data</param>
        /// <returns>Updated user</returns>
        [Authorize(Policy = "CanUpdateUsers")]
        [HttpPatch("{userId}/role")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserDto dto)
        {
            var user = await _userService.GetUserEntityByIdAsync(userId);
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

            // Check if role exists
            var roleExists = await _userService.RoleExistsAsync(dto.RoleId ?? 0);
            if (!roleExists)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = "Bad Request",
                    data = new { message = "Role tidak ditemukan atau tidak aktif" },
                    meta = (object?)null
                });
            }

            var updateDto = new UpdateUserDto
            {
                RoleId = dto.RoleId
            };

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, updateDto);
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

                var updatedUser = await _userService.GetUserByIdAsync(userId);

                HttpContext.Items["message"] = "Role user berhasil diperbarui";
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role {UserId}", userId);
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
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
        /// Upload photo profile untuk user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="dto">Photo file</param>
        /// <returns>Updated photo URL</returns>
        [Authorize]
        [HttpPost("{userId}/photo")]
        [ProducesResponseType(typeof(UpdatePhotoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadUserPhoto(int userId, [FromForm] UploadPhotoDto dto)
        {
            try
            {
                // Check if user can update their own photo or has permission to update others
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var canUpdateOthers = User.HasClaim("Permission", "user.update");

                if (userId.ToString() != currentUserId && !canUpdateOthers)
                {
                    return Forbid();
                }

                // Get existing user
                var user = await _userService.GetUserEntityByIdAsync(userId);
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

                // Delete old photo if exists
                if (!string.IsNullOrEmpty(user.PhotoUrl))
                {
                    var publicId = _cloudinaryService.GetPublicIdFromUrl(user.PhotoUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                        _logger.LogInformation("Old photo deleted from Cloudinary: {PublicId}", publicId);
                    }
                }

                // Upload new photo
                var photoUrl = await _cloudinaryService.UploadImageAsync(dto.Photo, $"profile/{userId}");
                if (string.IsNullOrEmpty(photoUrl))
                {
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Gagal upload photo",
                        data = new { message = "Photo tidak dapat diupload. Silakan coba lagi." },
                        meta = (object?)null
                    });
                }

                _logger.LogInformation("New photo uploaded to Cloudinary: {PhotoUrl}", photoUrl);

                // ✅ Update user photo URL
                var updated = await _userService.UpdateUserPhotoAsync(userId, photoUrl);
                if (!updated)
                {
                    // Rollback: delete uploaded photo if database update fails
                    var publicId = _cloudinaryService.GetPublicIdFromUrl(photoUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }

                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Gagal update photo",
                        data = new { message = "Gagal menyimpan photo ke database" },
                        meta = (object?)null
                    });
                }

                _logger.LogInformation("Photo URL saved to database for user: {UserId}", userId);

                // ✅ Return updated photo URL
                HttpContext.Items["message"] = "Photo profile berhasil diupload";
                return Ok(new UpdatePhotoResponseDto
                {
                    PhotoUrl = photoUrl,
                    Message = "Photo profile berhasil diupload"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for user {UserId}", userId);
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
        /// Delete photo profile user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Status deletion</returns>
        [HttpDelete("{userId}/photo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUserPhoto(int userId)
        {
            try
            {
                // Check if user can delete their own photo or has permission to update others
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var canUpdateOthers = User.HasClaim("Permission", "user.update");

                if (userId.ToString() != currentUserId && !canUpdateOthers)
                {
                    return Forbid();
                }

                // Get existing user
                var user = await _userService.GetUserEntityByIdAsync(userId);
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

                // Check if user has photo
                if (string.IsNullOrEmpty(user.PhotoUrl))
                {
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "User tidak memiliki photo",
                        data = new { },
                        meta = (object?)null
                    });
                }

                // Delete photo from Cloudinary
                var publicId = _cloudinaryService.GetPublicIdFromUrl(user.PhotoUrl);
                if (!string.IsNullOrEmpty(publicId))
                {
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }

                // Remove photo URL from user
                var updated = await _userService.UpdateUserPhotoAsync(userId, null);
                if (!updated)
                {
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Gagal hapus photo",
                        data = new { message = "Gagal menghapus photo dari database" },
                        meta = (object?)null
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Photo profile berhasil dihapus",
                    data = new { },
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo for user {UserId}", userId);
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message,
                    data = new { },
                    meta = (object?)null
                });
            }
        }
    }
}