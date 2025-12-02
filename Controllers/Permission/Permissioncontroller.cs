using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;
using Pm.Helper;

namespace Pm.Controllers
{
    [Route("api/permissions")]
    [ApiController]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(
            IPermissionService permissionService,
            ILogger<PermissionController> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _permissionService.GetAllPermissionsAsync();
            return ApiResponse.Success(permissions, "Daftar permissions berhasil dimuat");
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-group/{group}")]
        public async Task<IActionResult> GetPermissionsByGroup(string group)
        {
            var permissions = await _permissionService.GetPermissionsByGroupAsync(group);
            return ApiResponse.Success(permissions, $"Permissions untuk group '{group}' berhasil dimuat");
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("groups")]
        public async Task<IActionResult> GetPermissionGroups()
        {
            var groups = await _permissionService.GetPermissionGroupsAsync();
            return ApiResponse.Success(groups, "Permission groups berhasil dimuat");
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-role/{roleId}")]
        public async Task<IActionResult> GetPermissionsByRole(int roleId)
        {
            var permissions = await _permissionService.GetPermissionsByRoleAsync(roleId);
            if (permissions == null)
            {
                return ApiResponse.NotFound("Role tidak ditemukan");
            }

            return ApiResponse.Success(permissions, "Permissions untuk role berhasil dimuat");
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetPermissionsByUser(int userId)
        {
            var permissions = await _permissionService.GetPermissionsByUserAsync(userId);
            if (permissions == null)
            {
                return ApiResponse.NotFound("User tidak ditemukan atau tidak memiliki role");
            }

            return ApiResponse.Success(permissions, "Permissions untuk user berhasil dimuat");
        }

        [Authorize(Policy = "CanCreatePermission")]
        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var permission = await _permissionService.CreatePermissionAsync(dto);
                return ApiResponse.Created(permission, "Permission berhasil dibuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanEditPermission")]
        [HttpPut("{permissionId}")]
        public async Task<IActionResult> UpdatePermission(int permissionId, [FromBody] UpdatePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var permission = await _permissionService.UpdatePermissionAsync(permissionId, dto);
                if (permission == null)
                {
                    return ApiResponse.NotFound("Permission tidak ditemukan");
                }

                return ApiResponse.Success(permission, "Permission berhasil diperbarui");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission: {PermissionId}", permissionId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanEditPermission")]
        [HttpDelete("{permissionId}")]
        public async Task<IActionResult> DeletePermission(int permissionId)
        {
            try
            {
                var result = await _permissionService.DeletePermissionAsync(permissionId);
                if (!result)
                {
                    return ApiResponse.NotFound("Permission tidak ditemukan");
                }

                return ApiResponse.Success(new { }, "Permission berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission: {PermissionId}", permissionId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }
    }
}