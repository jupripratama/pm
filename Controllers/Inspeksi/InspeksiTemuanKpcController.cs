// Controllers/InspeksiTemuanKpcController.cs - UPDATE POLICIES
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;
using System.Security.Claims;

namespace Pm.Controllers
{
    [Route("api/inspeksi-temuan-kpc")]
    [ApiController]
    public class InspeksiTemuanKpcController : ControllerBase
    {
        private readonly IInspeksiTemuanKpcService _service;
        private readonly IEmailService _emailService;
        private readonly int _userId;

        public InspeksiTemuanKpcController(IInspeksiTemuanKpcService service, IEmailService emailService, IHttpContextAccessor http)
        {
            _service = service;
            _emailService = emailService;
            _userId = int.Parse(http.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: api/inspeksi-temuan-kpc
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] InspeksiTemuanKpcQueryDto query)
        {
            var result = await _service.GetAllAsync(query);
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
            query.IncludeDeleted = true;
            var result = await _service.GetAllAsync(query);
            return Ok(result);
        }

        // POST: api/inspeksi-temuan-kpc
        [Authorize(Policy = "InspeksiTemuanKpcCreate")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateInspeksiTemuanKpcDto dto)
        {
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
                    // Log error tapi jangan fail request
                    Console.WriteLine($"Email send failed: {ex.Message}");
                }
            }

            return Ok(new { message = "Temuan berhasil dibuat", id });
        }

        // PATCH: Update
        [Authorize(Policy = "InspeksiTemuanKpcUpdate")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateInspeksiTemuanKpcDto dto)
        {
            var success = await _service.UpdateAsync(id, dto, _userId);
            if (!success) return NotFound("Data tidak ditemukan atau sudah dihapus");

            // Kirim email kalau status jadi "Closed" (opsional)
            if (dto.Status?.Equals("Closed", StringComparison.OrdinalIgnoreCase) == true)
            {
                var entity = await _service.GetByIdAsync(id);
                if (entity != null && !string.IsNullOrEmpty(dto.PicPelaksana))
                {
                    try
                    {
                        await _emailService.SendStatusClosedEmailAsync(
                            temuanId: id,
                            ruang: entity.Ruang,
                            picEmail: dto.PicPelaksana
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email send failed: {ex.Message}");
                    }
                }
            }

            return Ok(new { message = "Temuan berhasil diperbarui" });
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