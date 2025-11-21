// Controllers/InspeksiTemuanKpcController.cs - UPDATE POLICIES
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.DTOs;
using Pm.Services;
using System.Security.Claims;
using Pm.DTOs.Common;

namespace Pm.Controllers
{
    [Route("api/inspeksi-temuan-kpc")]
    [ApiController]
    public class InspeksiTemuanKpcController : ControllerBase
    {
        private readonly IInspeksiTemuanKpcService _service;
        private readonly IEmailService _emailService;
        private readonly ILogger<InspeksiTemuanKpcController> _logger;
        private readonly int _userId;

        public InspeksiTemuanKpcController(
            IInspeksiTemuanKpcService service,
            IEmailService emailService,
            IHttpContextAccessor http,
            ILogger<InspeksiTemuanKpcController> logger)
        {
            _service = service;
            _emailService = emailService;
            _logger = logger;
            _userId = int.Parse(http.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: api/inspeksi-temuan-kpc
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] InspeksiTemuanKpcQueryDto query)
        {
            // ✅ SELALU set IncludeDeleted = false untuk active data
            query.IncludeDeleted = false;
            
            _logger.LogInformation("📊 GetAll request - Page: {Page}, PageSize: {PageSize}, IncludeDeleted: {IncludeDeleted}",
                query.Page, query.PageSize, query.IncludeDeleted);
            
            var result = await _service.GetAllAsync(query);
            
            // ✅ DEBUG LOG - VERIFY RESPONSE STRUCTURE
            _logger.LogInformation("📊 GetAll response - TotalCount: {TotalCount}, DataLength: {DataLength}, Page: {Page}, TotalPages: {TotalPages}",
                result.TotalCount, result.Data.Count, result.Page, result.TotalPages);
            
            return Ok(result);
        }

        // GET: api/inspeksi-temuan-kpc/5
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound("Data tidak ditemukan");
            return Ok(result);
        }

        // GET: api/inspeksi-temuan-kpc/history
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] InspeksiTemuanKpcQueryDto query)
        {
            // ✅ SET IncludeDeleted = true UNTUK MENDAPATKAN DATA DELETED
            query.IncludeDeleted = true;
            var result = await _service.GetAllAsync(query);

            // ✅ DEBUG: Log hasil sebelum dan sesudah filter
            _logger.LogInformation("📊 HISTORY DEBUG - Total from service: {Total}, Data count: {DataCount}",
                result.TotalCount, result.Data.Count);

            // ✅ FILTER MANUAL: Hanya kembalikan data yang Status = "Archived"
            var deletedItems = result.Data.Where(x => x.Status == "Archived").ToList();

            _logger.LogInformation("📊 HISTORY DEBUG - After filter: {FilteredCount}", deletedItems.Count);

            // ✅ DEBUG: List ID yang termasuk deleted
            foreach (var item in deletedItems)
            {
                _logger.LogInformation("📋 Deleted Item - ID: {Id}, Status: {Status}", item.Id, item.Status);
            }

            return Ok(new PagedResultDto<InspeksiTemuanKpcDto>(
                deletedItems,
                query,
                deletedItems.Count
            ));
        }
        // POST: api/inspeksi-temuan-kpc
        [Authorize(Policy = "InspeksiTemuanKpcCreate")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateInspeksiTemuanKpcDto dto)
        {
            _logger.LogInformation("🔄 Create request for room: {Ruang}", dto.Ruang);
            _logger.LogInformation("📁 Files received: {Count}", dto.FotoTemuanFiles?.Count ?? 0);

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var id = await _service.CreateAsync(dto, _userId);

            // KIRIM EMAIL KE PIC (opsional)
            if (!string.IsNullOrEmpty(dto.PicPelaksana))
            {
                try
                {
                    await _emailService.SendTemuanCreatedEmailAsync(
                        temuanId: id,
                        ruang: dto.Ruang,
                        temuan: dto.Temuan,
                        picEmail: dto.PicPelaksana
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Email send failed: {Message}", ex.Message);
                }
            }

            return Ok(new { message = "Temuan berhasil dibuat", id });
        }

        // PATCH: Update
        [Authorize(Policy = "InspeksiTemuanKpcUpdate")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateInspeksiTemuanKpcDto dto)
        {
            _logger.LogInformation("🔄 Update request for ID: {Id}", id);
            _logger.LogInformation("📁 Files received: {Count}", dto.FotoHasilFiles?.Count ?? 0);

            // ✅ DEBUG: LOG RECEIVED DATA
            _logger.LogInformation("📊 Received DTO values - Status: {Status}, NoFollowUp: {NoFollowUp}, PicPelaksana: {PicPelaksana}, Keterangan: {Keterangan}",
                dto.Status, dto.NoFollowUp, dto.PicPelaksana, dto.Keterangan);

            var updatedDto = await _service.UpdateAsync(id, dto, _userId);
            if (updatedDto == null) return NotFound("Data tidak ditemukan atau sudah dihapus");

            // Kirim email kalau status jadi "Closed" (opsional)
            if (dto.Status?.Equals("Closed", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!string.IsNullOrEmpty(dto.PicPelaksana))
                {
                    try
                    {
                        await _emailService.SendStatusClosedEmailAsync(
                            temuanId: id,
                            ruang: updatedDto.Ruang,
                            picEmail: dto.PicPelaksana
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Email send failed: {Message}", ex.Message);
                    }
                }
            }

            // ✅ RETURN WITHOUT EXTRA WRAPPER - LET MIDDLEWARE HANDLE IT
            return Ok(updatedDto);
        }

        // DELETE: api/inspeksi-temuan-kpc/5
        [Authorize(Policy = "InspeksiTemuanKpcDelete")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id, _userId);
            if (!success) return NotFound("Data tidak ditemukan");
            return Ok(new { message = "Temuan dipindah ke history" });
        }

        [Authorize(Policy = "InspeksiTemuanKpcDelete")] // Same policy as delete
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            try
            {
                var success = await _service.DeletePermanentAsync(id, _userId);
                if (!success) return NotFound("Data tidak ditemukan atau belum dipindahkan ke history");
                return Ok(new { message = "Temuan dihapus permanen dari database" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permanent for ID: {Id}", id);
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus data permanen" });
            }
        }

        // PATCH: api/inspeksi-temuan-kpc/5/restore
        [Authorize(Policy = "InspeksiTemuanKpcRestore")]
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var success = await _service.RestoreAsync(id, _userId);
            if (!success) return NotFound("Data tidak ditemukan atau belum dihapus");
            return Ok(new { message = "Temuan dikembalikan" });
        }

        // GET: api/inspeksi-temuan-kpc/export
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] bool history = false,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? ruang = null,
            [FromQuery] string? status = null)
        {
            var bytes = await _service.ExportToExcelAsync(history, startDate, endDate, ruang, status);
            var fileName = history ? $"History_KPC_{DateTime.Now:yyyyMMdd}.xlsx" : $"Laporan_KPC_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}