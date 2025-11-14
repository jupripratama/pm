using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;

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

        /// <summary>
        /// Get all permissions
        /// </summary>
        /// <returns>List of all permissions</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _permissionService.GetAllPermissionsAsync();
            HttpContext.Items["message"] = "Daftar permissions berhasil dimuat";
            return Ok(permissions);
        }

        /// <summary>
        /// Get permissions by group
        /// </summary>
        /// <param name="group">Permission group name</param>
        /// <returns>List of permissions in the group</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-group/{group}")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionsByGroup(string group)
        {
            var permissions = await _permissionService.GetPermissionsByGroupAsync(group);
            HttpContext.Items["message"] = $"Permissions untuk group '{group}' berhasil dimuat";
            return Ok(permissions);
        }

        /// <summary>
        /// Get all permission groups
        /// </summary>
        /// <returns>List of permission group names</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("groups")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionGroups()
        {
            var groups = await _permissionService.GetPermissionGroupsAsync();
            HttpContext.Items["message"] = "Permission groups berhasil dimuat";
            return Ok(groups);
        }

        /// <summary>
        /// Get permissions by role ID
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>List of permissions for the role</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-role/{roleId}")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionsByRole(int roleId)
        {
            var permissions = await _permissionService.GetPermissionsByRoleAsync(roleId);
            if (permissions == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "Role tidak ditemukan" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Permissions untuk role berhasil dimuat";
            return Ok(permissions);
        }

        /// <summary>
        /// Get permissions by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of permissions for the user</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-user/{userId}")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionsByUser(int userId)
        {
            var permissions = await _permissionService.GetPermissionsByUserAsync(userId);
            if (permissions == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "User tidak ditemukan atau tidak memiliki role" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Permissions untuk user berhasil dimuat";
            return Ok(permissions);
        }

        /// <summary>
        /// Create new permission
        /// </summary>
        /// <param name="dto">Permission data</param>
        /// <returns>Created permission</returns>
        [Authorize(Policy = "CanEditPermission")]
        [HttpPost]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var permission = await _permissionService.CreatePermissionAsync(dto);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    statusCode = StatusCodes.Status201Created,
                    message = "Permission berhasil dibuat",
                    data = permission,
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
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
        /// Update existing permission
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <param name="dto">Updated permission data</param>
        /// <returns>Updated permission</returns>
        [Authorize(Policy = "CanEditPermission")]
        [HttpPut("{permissionId}")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePermission(int permissionId, [FromBody] UpdatePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var permission = await _permissionService.UpdatePermissionAsync(permissionId, dto);
                if (permission == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "Permission tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                HttpContext.Items["message"] = "Permission berhasil diperbarui";
                return Ok(permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission: {PermissionId}", permissionId);
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
        /// Delete permission
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <returns>Status of deletion</returns>
        [Authorize(Policy = "CanEditPermission")]
        [HttpDelete("{permissionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeletePermission(int permissionId)
        {
            try
            {
                var result = await _permissionService.DeletePermissionAsync(permissionId);
                if (!result)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "Permission tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Permission berhasil dihapus",
                    data = new { },
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission: {PermissionId}", permissionId);
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