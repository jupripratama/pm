using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.CallRecord;
using Pm.Services;

namespace Pm.Controllers
{
    [Route("api/call-records")]
    [ApiController]
    [Produces("application/json")]
    public class CallRecordController : ControllerBase
    {
        private readonly ICallRecordService _callRecordService;
        private readonly ILogger<CallRecordController> _logger;

        public CallRecordController(ICallRecordService callRecordService, ILogger<CallRecordController> logger)
        {
            _callRecordService = callRecordService;
            _logger = logger;
        }

        /// <summary>
        /// Upload dan import file CSV call records
        /// </summary>
        [HttpPost("import-csv")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File tidak boleh kosong" });
            }

            if (!file.ContentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase) && 
                !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "File harus berformat CSV" });
            }

            if (file.Length > 50 * 1024 * 1024) // 50MB limit
            {
                return BadRequest(new { message = "Ukuran file maksimal 50MB" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _callRecordService.ImportCsvAsync(stream, file.FileName);

                var message = result.SuccessfulRecords > 0 
                    ? $"Import berhasil. {result.SuccessfulRecords} record berhasil diproses" 
                    : "Import gagal";

                if (result.FailedRecords > 0)
                {
                    message += $", {result.FailedRecords} record gagal diproses";
                }

                return Ok(new
                {
                    message = message,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV file {FileName}", file.FileName);
                return StatusCode(500, new { message = "Terjadi kesalahan saat mengimport file", error = ex.Message });
            }
        }

        /// <summary>
        /// Get call records dengan pagination dan filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCallRecords([FromQuery] CallRecordQueryDto query)
        {
            var result = await _callRecordService.GetCallRecordsAsync(query);
            return Ok(new
            {
                message = "Data call records berhasil dimuat",
                data = result
            });
        }

        /// <summary>
        /// Get hourly summary untuk tanggal tertentu
        /// </summary>
        [HttpGet("summary/hourly/{date}")]
        public async Task<IActionResult> GetHourlySummary([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(new { message = "Format tanggal tidak valid. Gunakan format YYYY-MM-DD" });
            }

            var result = await _callRecordService.GetHourlySummaryAsync(parsedDate);
            return Ok(new
            {
                message = $"Summary per jam untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dimuat",
                data = result
            });
        }

        /// <summary>
        /// Get daily summary untuk tanggal tertentu
        /// </summary>
        [HttpGet("summary/daily/{date}")]
        public async Task<IActionResult> GetDailySummary([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(new { message = "Format tanggal tidak valid. Gunakan format YYYY-MM-DD" });
            }

            var result = await _callRecordService.GetDailySummaryAsync(parsedDate);
            return Ok(new
            {
                message = $"Summary harian untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dimuat",
                data = result
            });
        }

        /// <summary>
        /// Get overall summary dengan semua jenis average calculations
        /// </summary>
        [HttpGet("summary/overall")]
        public async Task<IActionResult> GetOverallSummary(
            [FromQuery] string startDate, 
            [FromQuery] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate))
            {
                return BadRequest(new { message = "Format startDate tidak valid. Gunakan format YYYY-MM-DD" });
            }

            if (!DateTime.TryParse(endDate, out var parsedEndDate))
            {
                return BadRequest(new { message = "Format endDate tidak valid. Gunakan format YYYY-MM-DD" });
            }

            if (parsedStartDate > parsedEndDate)
            {
                return BadRequest(new { message = "StartDate tidak boleh lebih besar dari endDate" });
            }

            if ((parsedEndDate - parsedStartDate).Days > 90)
            {
                return BadRequest(new { message = "Rentang tanggal maksimal 90 hari" });
            }

            var result = await _callRecordService.GetOverallSummaryAsync(parsedStartDate, parsedEndDate);
            return Ok(new
            {
                message = $"Overall summary dari {parsedStartDate:yyyy-MM-dd} sampai {parsedEndDate:yyyy-MM-dd} berhasil dimuat",
                data = result
            });
        }

        /// <summary>
        /// Regenerate summaries untuk rentang tanggal tertentu
        /// </summary>
        [HttpPost("summary/regenerate")]
        public async Task<IActionResult> RegenerateSummaries(
            [FromQuery] string startDate, 
            [FromQuery] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate))
            {
                return BadRequest(new { message = "Format startDate tidak valid. Gunakan format YYYY-MM-DD" });
            }

            if (!DateTime.TryParse(endDate, out var parsedEndDate))
            {
                return BadRequest(new { message = "Format endDate tidak valid. Gunakan format YYYY-MM-DD" });
            }

            try
            {
                var success = await _callRecordService.RegenerateSummariesAsync(parsedStartDate, parsedEndDate);
                
                if (success)
                {
                    return Ok(new
                    {
                        message = $"Summary berhasil di-regenerate dari {parsedStartDate:yyyy-MM-dd} sampai {parsedEndDate:yyyy-MM-dd}"
                    });
                }
                else
                {
                    return StatusCode(500, new { message = "Gagal melakukan regenerate summary" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating summaries");
                return StatusCode(500, new { message = "Terjadi kesalahan saat regenerate summary", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete call records untuk tanggal tertentu
        /// </summary>
        [HttpDelete("{date}")]
        public async Task<IActionResult> DeleteCallRecords([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(new { message = "Format tanggal tidak valid. Gunakan format YYYY-MM-DD" });
            }

            try
            {
                var success = await _callRecordService.DeleteCallRecordsAsync(parsedDate);
                
                if (success)
                {
                    return Ok(new
                    {
                        message = $"Call records untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dihapus"
                    });
                }
                else
                {
                    return StatusCode(500, new { message = "Gagal menghapus call records" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call records");
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus call records", error = ex.Message });
            }
        }
    }
}