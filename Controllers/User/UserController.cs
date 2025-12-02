using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Services;
using FluentValidation;
using Pm.Helper;

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

        [Authorize(Policy = "CanViewUsers")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryDto dto)
        {
            var users = await _userService.GetUsersAsync(dto);
            return ApiResponse.Success(users, "Daftar user berhasil dimuat");
        }

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
                return ApiResponse.NotFound("User tidak ditemukan");
            }
            return ApiResponse.Success(user, "User berhasil dimuat");
        }

        [Authorize(Policy = "CanCreateUsers")]
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
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
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var user = await _userService.CreateUserAsync(dto);
                if (user == null)
                {
                    return ApiResponse.BadRequest("message", "Username atau email mungkin sudah digunakan");
                }
                return ApiResponse.Created(user, "User berhasil dibuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

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
                return ApiResponse.NotFound("User tidak ditemukan");
            }

            var updateDto = new UpdateUserDto { IsActive = true };

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, updateDto);
                if (!updated)
                {
                    return ApiResponse.NotFound("User tidak ditemukan");
                }

                var updatedUser = await _userService.GetUserByIdAsync(userId);
                return ApiResponse.Success(updatedUser, "User berhasil diaktifkan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

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
                return ApiResponse.NotFound("User tidak ditemukan");
            }

            var updateDto = new UpdateUserDto { IsActive = false };

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, updateDto);
                if (!updated)
                {
                    return ApiResponse.NotFound("User tidak ditemukan");
                }

                var updatedUser = await _userService.GetUserByIdAsync(userId);
                return ApiResponse.Success(updatedUser, "User berhasil dinonaktifkan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

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
                return ApiResponse.NotFound("User tidak ditemukan");
            }

            var roleExists = await _userService.RoleExistsAsync(dto.RoleId ?? 0);
            if (!roleExists)
            {
                return ApiResponse.BadRequest("message", "Role tidak ditemukan atau tidak aktif");
            }

            var updateDto = new UpdateUserDto { RoleId = dto.RoleId };

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, updateDto);
                if (!updated)
                {
                    return ApiResponse.NotFound("User tidak ditemukan");
                }

                var updatedUser = await _userService.GetUserByIdAsync(userId);
                return ApiResponse.Success(updatedUser, "Role user berhasil diperbarui");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanUpdateUsers")]
        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserDto dto)
        {
            var existingUser = await _userService.GetUserEntityByIdAsync(userId);
            if (existingUser == null)
            {
                return ApiResponse.NotFound("User tidak ditemukan");
            }

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
                return BadRequest(new { data = errors });
            }

            bool isSame =
                (dto.FullName == null || dto.FullName == existingUser.FullName) &&
                (dto.Username == null || dto.Username == existingUser.Username) &&
                (dto.Email == null || dto.Email == existingUser.Email) &&
                (dto.RoleId == null || dto.RoleId == existingUser.RoleId) &&
                (dto.IsActive == null || dto.IsActive == existingUser.IsActive);

            if (isSame)
            {
                return ApiResponse.BadRequest("message", "Tidak ada field yang berubah");
            }

            try
            {
                var updated = await _userService.UpdateUserAsync(userId, dto);
                if (!updated)
                {
                    return ApiResponse.NotFound("User tidak ditemukan");
                }

                var user = await _userService.GetUserByIdAsync(userId);
                return ApiResponse.Success(user, "User berhasil diperbarui");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

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
                    return ApiResponse.NotFound("User tidak ditemukan");
                }
                return ApiResponse.Success(new { }, "User berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

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
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var canUpdateOthers = User.HasClaim("Permission", "user.update");

                if (userId.ToString() != currentUserId && !canUpdateOthers)
                {
                    return ApiResponse.Forbidden();
                }

                var user = await _userService.GetUserEntityByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse.NotFound("User tidak ditemukan");
                }

                if (!string.IsNullOrEmpty(user.PhotoUrl))
                {
                    var publicId = _cloudinaryService.GetPublicIdFromUrl(user.PhotoUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                        _logger.LogInformation("Old photo deleted from Cloudinary: {PublicId}", publicId);
                    }
                }

                var photoUrl = await _cloudinaryService.UploadImageAsync(dto.Photo, $"profile/{userId}");
                if (string.IsNullOrEmpty(photoUrl))
                {
                    return ApiResponse.BadRequest("message", "Photo tidak dapat diupload. Silakan coba lagi.");
                }

                _logger.LogInformation("New photo uploaded to Cloudinary: {PhotoUrl}", photoUrl);

                var updated = await _userService.UpdateUserPhotoAsync(userId, photoUrl);
                if (!updated)
                {
                    var publicId = _cloudinaryService.GetPublicIdFromUrl(photoUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                    return ApiResponse.BadRequest("message", "Gagal menyimpan photo ke database");
                }

                _logger.LogInformation("Photo URL saved to database for user: {UserId}", userId);

                return ApiResponse.Success(
                    new UpdatePhotoResponseDto
                    {
                        PhotoUrl = photoUrl,
                        Message = "Photo profile berhasil diupload"
                    },
                    "Photo profile berhasil diupload"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{userId}/photo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUserPhoto(int userId)
        {
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var canUpdateOthers = User.HasClaim("Permission", "user.update");

                if (userId.ToString() != currentUserId && !canUpdateOthers)
                {
                    return ApiResponse.Forbidden();
                }

                var user = await _userService.GetUserEntityByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse.NotFound("User tidak ditemukan");
                }

                if (string.IsNullOrEmpty(user.PhotoUrl))
                {
                    return ApiResponse.BadRequest("message", "User tidak memiliki photo");
                }

                var publicId = _cloudinaryService.GetPublicIdFromUrl(user.PhotoUrl);
                if (!string.IsNullOrEmpty(publicId))
                {
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }

                var updated = await _userService.UpdateUserPhotoAsync(userId, null);
                if (!updated)
                {
                    return ApiResponse.BadRequest("message", "Gagal menghapus photo dari database");
                }

                return ApiResponse.Success(new { }, "Photo profile berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo for user {UserId}", userId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }
    }
}