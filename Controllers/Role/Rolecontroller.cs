using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;
using Pm.Helper;

namespace Pm.Controllers
{
    [Route("api/roles")]
    [ApiController]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IRoleService roleService,
            ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        [Authorize(Policy = "CanViewRoles")]
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return ApiResponse.Success(roles, "Daftar roles berhasil dimuat");
        }

        [Authorize(Policy = "CanViewDetailRoles")]
        [HttpGet("{roleId}")]
        public async Task<IActionResult> GetRoleById(int roleId)
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                return ApiResponse.NotFound("Role tidak ditemukan");
            }

            return ApiResponse.Success(role, "Role berhasil dimuat");
        }

        [Authorize(Policy = "CanCreateRoles")]
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var role = await _roleService.CreateRoleAsync(dto);
                return ApiResponse.Created(role, "Role berhasil dibuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanUpdateRoles")]
        [HttpPut("{roleId}")]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] UpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var role = await _roleService.UpdateRoleAsync(roleId, dto);
                if (role == null)
                {
                    return ApiResponse.NotFound("Role tidak ditemukan");
                }

                return ApiResponse.Success(role, "Role berhasil diperbarui");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role: {RoleId}", roleId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanDeleteRoles")]
        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            try
            {
                var result = await _roleService.DeleteRoleAsync(roleId);
                if (!result)
                {
                    return ApiResponse.NotFound("Role tidak ditemukan");
                }

                return ApiResponse.Success(new { }, "Role berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role: {RoleId}", roleId);
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanViewDetailRoles")]
        [HttpGet("{roleId}/permissions")]
        public async Task<IActionResult> GetRoleWithPermissions(int roleId)
        {
            var role = await _roleService.GetRoleWithPermissionsAsync(roleId);
            if (role == null)
            {
                return ApiResponse.NotFound("Role tidak ditemukan");
            }

            return ApiResponse.Success(role, "Role dengan permissions berhasil dimuat");
        }
    }
}