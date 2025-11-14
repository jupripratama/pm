using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;

namespace Pm.Controllers
{
    [Route("api/role-permissions")]
    [ApiController]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class RolePermissionController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;
        private readonly ILogger<RolePermissionController> _logger;

        public RolePermissionController(
            IRolePermissionService rolePermissionService,
            ILogger<RolePermissionController> logger)
        {
            _rolePermissionService = rolePermissionService;
            _logger = logger;
        }

        /// <summary>
        /// Get all role-permission mappings
        /// </summary>
        /// <returns>List of all role-permission mappings</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet]
        [ProducesResponseType(typeof(List<RolePermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var rolePermissions = await _rolePermissionService.GetAllRolePermissionsAsync();
            HttpContext.Items["message"] = "Role permissions berhasil dimuat";
            return Ok(rolePermissions);
        }

        /// <summary>
        /// Get permissions for specific role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>List of permissions for the role</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-role/{roleId}")]
        [ProducesResponseType(typeof(RolePermissionDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionsByRole(int roleId)
        {
            var result = await _rolePermissionService.GetPermissionsByRoleAsync(roleId);
            if (result == null)
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
            return Ok(result);
        }

        /// <summary>
        /// Add single permission to role
        /// </summary>
        /// <param name="dto">Role and Permission IDs</param>
        /// <returns>Created role permission</returns>
        [Authorize(Policy = "CanEditPermissions")]
        [HttpPost]
        [ProducesResponseType(typeof(RolePermissionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddPermissionToRole([FromBody] CreateRolePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _rolePermissionService.AddPermissionToRoleAsync(dto);
                if (result == null)
                {
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Permission sudah ada untuk role ini",
                        data = new { },
                        meta = (object?)null
                    });
                }

                return StatusCode(StatusCodes.Status201Created, new
                {
                    statusCode = StatusCodes.Status201Created,
                    message = "Permission berhasil ditambahkan ke role",
                    data = result,
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding permission to role");
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
        /// Batch update permissions for a role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="dto">List of permission IDs</param>
        /// <returns>Updated permissions for the role</returns>
        [Authorize(Policy = "CanEditPermissions")]
        [HttpPut("role/{roleId}")]
        [ProducesResponseType(typeof(RolePermissionDetailDto), StatusCodes.Status200OK)]
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
                var result = await _rolePermissionService.UpdateRolePermissionsAsync(roleId, dto.PermissionIds);
                if (result == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "Role tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                HttpContext.Items["message"] = "Permissions untuk role berhasil diperbarui";
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permissions");
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
        /// Remove single permission from role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="permissionId">Permission ID</param>
        /// <returns>Status of deletion</returns>
        [Authorize(Policy = "CanEditPermissions")]
        [HttpDelete("role/{roleId}/permission/{permissionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemovePermissionFromRole(int roleId, int permissionId)
        {
            try
            {
                var result = await _rolePermissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
                if (!result)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "Role permission tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Permission berhasil dihapus dari role",
                    data = new { },
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission from role");
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
        /// Get roles that have specific permission
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <returns>List of roles with the permission</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-permission/{permissionId}")]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRolesByPermission(int permissionId)
        {
            var roles = await _rolePermissionService.GetRolesByPermissionAsync(permissionId);
            if (roles == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "Permission tidak ditemukan" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Roles untuk permission berhasil dimuat";
            return Ok(roles);
        }

        /// <summary>
        /// Get permission matrix (all roles with their permissions)
        /// </summary>
        /// <returns>Matrix of all roles and their permissions</returns>
        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("matrix")]
        [ProducesResponseType(typeof(List<RolePermissionMatrixDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPermissionMatrix()
        {
            var matrix = await _rolePermissionService.GetPermissionMatrixAsync();
            HttpContext.Items["message"] = "Permission matrix berhasil dimuat";
            return Ok(matrix);
        }
    }
}