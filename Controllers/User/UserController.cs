using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
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
        /// Mendapatkan semua permissions
        /// </summary>
        /// <returns>Daftar permissions</returns>
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
        /// Mendapatkan permissions berdasarkan group
        /// </summary>
        /// <param name="group">Nama group permission</param>
        /// <returns>Daftar permissions by group</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-group/{group}")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionsByGroup(string group)
        {
            var permissions = await _permissionService.GetPermissionsByGroupAsync(group);
            HttpContext.Items["message"] = $"Permissions untuk group {group} berhasil dimuat";
            return Ok(permissions);
        }

        /// <summary>
        /// Mendapatkan semua groups yang tersedia
        /// </summary>
        /// <returns>Daftar groups</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("groups")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionGroups()
        {
            var groups = await _permissionService.GetPermissionGroupsAsync();
            HttpContext.Items["message"] = "Daftar groups berhasil dimuat";
            return Ok(groups);
        }

        /// <summary>
        /// Mendapatkan permissions untuk role tertentu
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>Daftar permissions untuk role</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("role/{roleId}")]
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
        /// Update permissions untuk role tertentu
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="dto">Permission IDs yang akan di-assign</param>
        /// <returns>Status update</returns>
        [Authorize(Policy = "CanEditPermissions")]
        [HttpPut("role/{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _permissionService.UpdateRolePermissionsAsync(roleId, dto.PermissionIds);
                if (!result)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "Role tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                var updatedPermissions = await _permissionService.GetPermissionsByRoleAsync(roleId);

                return Ok(new
                {
                    statusCode = 200,
                    message = "Permissions berhasil diperbarui",
                    data = updatedPermissions,
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permissions for RoleId: {RoleId}", roleId);
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
        /// Mendapatkan permissions untuk user tertentu
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Daftar permissions user</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("user/{userId}")]
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
                    data = new { message = "User tidak ditemukan" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Permissions untuk user berhasil dimuat";
            return Ok(permissions);
        }

        /// <summary>
        /// Membuat permission baru (Super Admin only)
        /// </summary>
        /// <param name="dto">Data permission baru</param>
        /// <returns>Permission yang dibuat</returns>
        [Authorize(Policy = "IsSuperAdmin")]
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
        /// Update permission (Super Admin only)
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <param name="dto">Data permission yang diupdate</param>
        /// <returns>Permission yang diupdate</returns>
        [Authorize(Policy = "IsSuperAdmin")]
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
        /// Hapus permission (Super Admin only)
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <returns>Status penghapusan</returns>
        [Authorize(Policy = "IsSuperAdmin")]
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