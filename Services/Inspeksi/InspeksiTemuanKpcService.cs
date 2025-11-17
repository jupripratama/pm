// Services/InspeksiTemuanKpcService.cs - COMPLETE VERSION
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.DTOs.Export;
using Pm.Models;
using System.Text.Json;
using ClosedXML.Excel;

namespace Pm.Services
{
    public class InspeksiTemuanKpcService : IInspeksiTemuanKpcService
    {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private readonly IActivityLogService _log;
        private readonly ILogger<InspeksiTemuanKpcService> _logger;

        public InspeksiTemuanKpcService(AppDbContext context, ICloudinaryService cloudinary, IActivityLogService log, ILogger<InspeksiTemuanKpcService> logger)
        {
            _context = context;
            _cloudinary = cloudinary;
            _log = log;
            _logger = logger;
        }

        // === GET ALL + FILTER ===
        public async Task<PagedResultDto<InspeksiTemuanKpcDto>> GetAllAsync(InspeksiTemuanKpcQueryDto query)
        {
            var q = _context.InspeksiTemuanKpcs
                .Include(x => x.CreatedByUser)
                .Include(x => x.UpdatedByUser)
                .AsQueryable();

            // Soft delete filter
            q = query.IncludeDeleted ? q.Where(x => x.IsDeleted) : q.Where(x => !x.IsDeleted);

            // Filter lainnya
            if (!string.IsNullOrEmpty(query.Ruang)) q = q.Where(x => x.Ruang.Contains(query.Ruang));
            if (!string.IsNullOrEmpty(query.Status)) q = q.Where(x => x.Status == query.Status);
            if (query.StartDate.HasValue) q = q.Where(x => x.TanggalTemuan >= query.StartDate.Value.Date);
            if (query.EndDate.HasValue) q = q.Where(x => x.TanggalTemuan <= query.EndDate.Value.Date.AddDays(1));

            var total = await q.CountAsync();

            // Materialize query first so subsequent JSON deserialization runs in memory (not in EF expression tree)
            var entities = await q
                .OrderByDescending(x => x.TanggalTemuan)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var items = entities
                .Select(x => new InspeksiTemuanKpcDto
                {
                    Id = x.Id,
                    Ruang = x.Ruang,
                    Temuan = x.Temuan,
                    KategoriTemuan = x.KategoriTemuan ?? "-",
                    Inspector = x.Inspector ?? "-",
                    Severity = x.Severity,
                    TanggalTemuan = x.TanggalTemuan.ToString("dd MMM yyyy"),
                    NoFollowUp = x.NoFollowUp ?? "-",
                    FollowUpRef = x.FollowUpRef ?? "-",
                    PerbaikanDilakukan = x.PerbaikanDilakukan ?? "-",
                    TanggalPerbaikan = x.TanggalPerbaikan != null ? x.TanggalPerbaikan.Value.ToString("dd MMM yyyy") : "-",
                    TanggalSelesaiPerbaikan = x.TanggalSelesaiPerbaikan != null ? x.TanggalSelesaiPerbaikan.Value.ToString("dd MMM yyyy") : "-",
                    PicPelaksana = x.PicPelaksana ?? "-",
                    Status = x.IsDeleted ? "Archived" : x.Status,
                    Keterangan = x.Keterangan ?? "-",

                    // ✅ Parse JSON to List untuk display (runs in memory)
                    FotoTemuanUrls = string.IsNullOrEmpty(x.FotoTemuanUrls)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(x.FotoTemuanUrls) ?? new List<string>(),
                    FotoHasilUrls = string.IsNullOrEmpty(x.FotoHasilUrls)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(x.FotoHasilUrls) ?? new List<string>(),

                    FotoTemuan = GetFotoText(x.FotoTemuanUrls),
                    FotoHasil = GetFotoText(x.FotoHasilUrls),

                    CreatedByName = x.CreatedByUser != null ? x.CreatedByUser.FullName : "Unknown",
                    CreatedAt = x.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                    UpdatedByName = x.UpdatedByUser != null ? x.UpdatedByUser.FullName : "-",
                    UpdatedAt = x.UpdatedAt != null ? x.UpdatedAt.Value.ToString("dd MMM yyyy HH:mm") : "-"
                })
                .ToList();

            return new PagedResultDto<InspeksiTemuanKpcDto>(items, query, total);
        }

        // ✅ === GET BY ID - IMPLEMENT ===
        public async Task<InspeksiTemuanKpcDto?> GetByIdAsync(int id)
        {
            var item = await _context.InspeksiTemuanKpcs
                .Include(x => x.CreatedByUser)
                .Include(x => x.UpdatedByUser)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return null;

            return new InspeksiTemuanKpcDto
            {
                Id = item.Id,
                Ruang = item.Ruang,
                Temuan = item.Temuan,
                KategoriTemuan = item.KategoriTemuan ?? "-",
                Inspector = item.Inspector ?? "-",
                Severity = item.Severity,
                TanggalTemuan = item.TanggalTemuan.ToString("yyyy-MM-dd"), // Format untuk input date
                NoFollowUp = item.NoFollowUp ?? "-",
                FollowUpRef = item.FollowUpRef ?? "-",
                PerbaikanDilakukan = item.PerbaikanDilakukan ?? "-",
                TanggalPerbaikan = item.TanggalPerbaikan?.ToString("yyyy-MM-dd") ?? "",
                TanggalSelesaiPerbaikan = item.TanggalSelesaiPerbaikan?.ToString("yyyy-MM-dd") ?? "",
                PicPelaksana = item.PicPelaksana ?? "-",
                Status = item.Status,
                Keterangan = item.Keterangan ?? "-",

                // ✅ Parse JSON arrays
                FotoTemuanUrls = string.IsNullOrEmpty(item.FotoTemuanUrls)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(item.FotoTemuanUrls) ?? new List<string>(),
                FotoHasilUrls = string.IsNullOrEmpty(item.FotoHasilUrls)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(item.FotoHasilUrls) ?? new List<string>(),

                FotoTemuan = GetFotoText(item.FotoTemuanUrls),
                FotoHasil = GetFotoText(item.FotoHasilUrls),

                CreatedByName = item.CreatedByUser?.FullName ?? "Unknown",
                CreatedAt = item.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                UpdatedByName = item.UpdatedByUser?.FullName ?? "-",
                UpdatedAt = item.UpdatedAt?.ToString("dd MMM yyyy HH:mm") ?? "-"
            };
        }

        // ✅ === CREATE - UPDATED ===
        public async Task<int> CreateAsync(CreateInspeksiTemuanKpcDto dto, int userId)
        {
            // Upload multiple foto temuan
            var fotoTemuanUrls = new List<string>();
            if (dto.FotoTemuanFiles != null && dto.FotoTemuanFiles.Count > 0)
            {
                _logger.LogInformation($"Uploading {dto.FotoTemuanFiles.Count} foto temuan...");
                foreach (var file in dto.FotoTemuanFiles)
                {
                    var url = await _cloudinary.UploadImageAsync(file, "inspeksi/kpc/temuan");
                    if (url != null) fotoTemuanUrls.Add(url);
                }
                _logger.LogInformation($"Successfully uploaded {fotoTemuanUrls.Count} foto temuan");
            }

            var entity = new InspeksiTemuanKpc
            {
                Ruang = dto.Ruang,
                Temuan = dto.Temuan,
                KategoriTemuan = dto.KategoriTemuan,
                Inspector = dto.Inspector,  // ✅ TAMBAHAN
                Severity = dto.Severity,
                TanggalTemuan = dto.TanggalTemuan,
                NoFollowUp = dto.NoFollowUp,
                PicPelaksana = dto.PicPelaksana,
                Keterangan = dto.Keterangan,
                FotoTemuanUrls = fotoTemuanUrls.Count > 0 ? JsonSerializer.Serialize(fotoTemuanUrls) : null,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.InspeksiTemuanKpcs.Add(entity);
            await _context.SaveChangesAsync();

            await _log.LogAsync("InspeksiTemuanKpc", entity.Id, "Created", userId, $"Temuan baru di {dto.Ruang} oleh {dto.Inspector ?? "Unknown"}");

            return entity.Id;
        }

        // ✅ === UPDATE - UPDATED ===
        public async Task<bool> UpdateAsync(int id, UpdateInspeksiTemuanKpcDto dto, int userId)
        {
            var entity = await _context.InspeksiTemuanKpcs.FindAsync(id);
            if (entity == null || entity.IsDeleted) return false;

            var oldStatus = entity.Status;

            // Update fields
            if (dto.FollowUpRef != null) entity.FollowUpRef = dto.FollowUpRef;
            if (dto.PerbaikanDilakukan != null) entity.PerbaikanDilakukan = dto.PerbaikanDilakukan;
            if (dto.TanggalPerbaikan.HasValue) entity.TanggalPerbaikan = dto.TanggalPerbaikan;
            if (dto.TanggalSelesaiPerbaikan.HasValue) entity.TanggalSelesaiPerbaikan = dto.TanggalSelesaiPerbaikan;  // ✅ TAMBAHAN
            if (dto.PicPelaksana != null) entity.PicPelaksana = dto.PicPelaksana;
            if (dto.Keterangan != null) entity.Keterangan = dto.Keterangan;

            if (dto.Status != null)
            {
                entity.Status = dto.Status;
                if (dto.Status == "Closed" && entity.TanggalClosed == null)
                    entity.TanggalClosed = DateTime.UtcNow;
            }

            // ✅ Upload multiple foto hasil
            if (dto.FotoHasilFiles != null && dto.FotoHasilFiles.Count > 0)
            {
                _logger.LogInformation($"Uploading {dto.FotoHasilFiles.Count} foto hasil for ID {id}...");

                // Get existing URLs or create new list
                var existing = string.IsNullOrEmpty(entity.FotoHasilUrls)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(entity.FotoHasilUrls) ?? new List<string>();

                // Upload each new file
                foreach (var file in dto.FotoHasilFiles)
                {
                    var url = await _cloudinary.UploadImageAsync(file, "inspeksi/kpc/hasil");
                    if (url != null) existing.Add(url);
                }

                entity.FotoHasilUrls = JsonSerializer.Serialize(existing);
                _logger.LogInformation($"Total foto hasil now: {existing.Count}");
            }

            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _log.LogAsync("InspeksiTemuanKpc", id, "Updated", userId, $"Status: {oldStatus} → {entity.Status}");

            return true;
        }

        // === SOFT DELETE ===
        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var entity = await _context.InspeksiTemuanKpcs.FindAsync(id);
            if (entity == null || entity.IsDeleted) return false;

            entity.IsDeleted = true;
            entity.DeletedBy = userId;
            entity.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _log.LogAsync("InspeksiTemuanKpc", id, "Deleted", userId, "Dipindah ke history");
            return true;
        }

        // === RESTORE ===
        public async Task<bool> RestoreAsync(int id, int userId)
        {
            var entity = await _context.InspeksiTemuanKpcs.FindAsync(id);
            if (entity == null || !entity.IsDeleted) return false;

            entity.IsDeleted = false;
            entity.DeletedBy = null;
            entity.DeletedAt = null;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _log.LogAsync("InspeksiTemuanKpc", id, "Restored", userId, "Dikembalikan dari history");
            return true;
        }

        // === EXPORT EXCEL ===
        public async Task<byte[]> ExportToExcelAsync(bool history, DateTime? start, DateTime? end, string? ruang, string? status)
        {
            var query = new InspeksiTemuanKpcQueryDto
            {
                IncludeDeleted = history,
                Ruang = ruang,
                Status = status,
                StartDate = start,
                EndDate = end,
                Page = 1,
                PageSize = 10000 // Get all for export
            };

            var result = await GetAllAsync(query);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Laporan Inspeksi KPC");

            // Headers
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Ruang";
            ws.Cell(1, 3).Value = "Temuan";
            ws.Cell(1, 4).Value = "Kategori";
            ws.Cell(1, 5).Value = "Inspector";
            ws.Cell(1, 6).Value = "Severity";
            ws.Cell(1, 7).Value = "Tgl Temuan";
            ws.Cell(1, 8).Value = "No Follow Up";
            ws.Cell(1, 9).Value = "Follow Up Ref";
            ws.Cell(1, 10).Value = "Perbaikan";
            ws.Cell(1, 11).Value = "Tgl Perbaikan";
            ws.Cell(1, 12).Value = "Tgl Selesai";
            ws.Cell(1, 13).Value = "PIC";
            ws.Cell(1, 14).Value = "Status";
            ws.Cell(1, 15).Value = "Keterangan";
            ws.Cell(1, 16).Value = "Foto Temuan";
            ws.Cell(1, 17).Value = "Foto Hasil";
            ws.Cell(1, 18).Value = "Dibuat Oleh";
            ws.Cell(1, 19).Value = "Dibuat Pada";

            // Style header
            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data
            int row = 2;
            int no = 1;
            foreach (var item in result.Data)
            {
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.Ruang;
                ws.Cell(row, 3).Value = item.Temuan;
                ws.Cell(row, 4).Value = item.KategoriTemuan;
                ws.Cell(row, 5).Value = item.Inspector;
                ws.Cell(row, 6).Value = item.Severity;
                ws.Cell(row, 7).Value = item.TanggalTemuan;
                ws.Cell(row, 8).Value = item.NoFollowUp;
                ws.Cell(row, 9).Value = item.FollowUpRef;
                ws.Cell(row, 10).Value = item.PerbaikanDilakukan;
                ws.Cell(row, 11).Value = item.TanggalPerbaikan;
                ws.Cell(row, 12).Value = item.TanggalSelesaiPerbaikan;
                ws.Cell(row, 13).Value = item.PicPelaksana;
                ws.Cell(row, 14).Value = item.Status;
                ws.Cell(row, 15).Value = item.Keterangan;
                ws.Cell(row, 16).Value = item.FotoTemuan;
                ws.Cell(row, 17).Value = item.FotoHasil;
                ws.Cell(row, 18).Value = item.CreatedByName;
                ws.Cell(row, 19).Value = item.CreatedAt;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        // === HELPER GET FOTO TEXT ===
        private string GetFotoText(string? json)
        {
            if (string.IsNullOrEmpty(json)) return "-";
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);
                return list?.Count > 0 ? $"{list.Count} foto" : "-";
            }
            catch
            {
                return "Ada foto";
            }
        }
    }
}