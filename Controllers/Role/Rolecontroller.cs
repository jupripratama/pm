using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Services;

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

        /// <summary>
        /// Mendapatkan semua roles
        /// </summary>
        /// <returns>Daftar roles</returns>
        [Authorize(Policy = "CanViewRoles")]
        [HttpGet]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            HttpContext.Items["message"] = "Daftar roles berhasil dimuat";
            return Ok(roles);
        }

        /// <summary>
        /// Mendapatkan role berdasarkan ID
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>Detail role</returns>
        [Authorize(Policy = "CanViewDetailRoles")]
        [HttpGet("{roleId}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoleById(int roleId)
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "Role tidak ditemukan" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Role berhasil dimuat";
            return Ok(role);
        }

        /// <summary>
        /// Membuat role baru
        /// </summary>
        /// <param name="dto">Data role baru</param>
        /// <returns>Role yang dibuat</returns>
        [Authorize(Policy = "CanCreateRoles")]
        [HttpPost]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var role = await _roleService.CreateRoleAsync(dto);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    statusCode = StatusCodes.Status201Created,
                    message = "Role berhasil dibuat",
                    data = role,
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
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
        /// Update role yang sudah ada
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="dto">Data role yang diupdate</param>
        /// <returns>Role yang diupdate</returns>
        [Authorize(Policy = "CanUpdateRoles")]
        [HttpPut("{roleId}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] UpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var role = await _roleService.UpdateRoleAsync(roleId, dto);
                if (role == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "Not Found",
                        data = new { message = "Role tidak ditemukan" },
                        meta = (object?)null
                    });
                }

                HttpContext.Items["message"] = "Role berhasil diperbarui";
                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role: {RoleId}", roleId);
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
        /// Hapus role berdasarkan ID
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>Status penghapusan</returns>
        [Authorize(Policy = "CanDeleteRoles")]
        [HttpDelete("{roleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            try
            {
                var result = await _roleService.DeleteRoleAsync(roleId);
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

                return Ok(new
                {
                    statusCode = 200,
                    message = "Role berhasil dihapus",
                    data = new { },
                    meta = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role: {RoleId}", roleId);
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
        /// Mendapatkan role dengan permissions
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>Role dengan permissions</returns>
        [Authorize(Policy = "CanViewDetailRoles")]
        [HttpGet("{roleId}/permissions")]
        [ProducesResponseType(typeof(RoleWithPermissionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoleWithPermissions(int roleId)
        {
            var role = await _roleService.GetRoleWithPermissionsAsync(roleId);
            if (role == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = "Not Found",
                    data = new { message = "Role tidak ditemukan" },
                    meta = (object?)null
                });
            }

            HttpContext.Items["message"] = "Role dengan permissions berhasil dimuat";
            return Ok(role);
        }
    }
}