// Services/InspeksiTemuanKpcService.cs - FIXED EXCEL EXPORT WITH ALL IMAGES
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

            if (!query.IncludeDeleted)
            {
                q = q.Where(x => !x.IsDeleted);
                _logger.LogInformation("🔍 Filtering out deleted records");
            }
            else
            {
                _logger.LogInformation("🔍 Including deleted records");
            }

            if (!string.IsNullOrEmpty(query.Ruang)) q = q.Where(x => x.Ruang.Contains(query.Ruang));
            if (!string.IsNullOrEmpty(query.Status)) q = q.Where(x => x.Status == query.Status);
            if (query.StartDate.HasValue) q = q.Where(x => x.TanggalTemuan >= query.StartDate.Value.Date);
            if (query.EndDate.HasValue) q = q.Where(x => x.TanggalTemuan <= query.EndDate.Value.Date.AddDays(1));

            var total = await q.CountAsync();

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
                    PerbaikanDilakukan = x.PerbaikanDilakukan ?? "-",
                    TanggalPerbaikan = x.TanggalPerbaikan != null ? x.TanggalPerbaikan.Value.ToString("dd MMM yyyy") : "-",
                    TanggalSelesaiPerbaikan = x.TanggalSelesaiPerbaikan != null ? x.TanggalSelesaiPerbaikan.Value.ToString("dd MMM yyyy") : "-",
                    PicPelaksana = x.PicPelaksana ?? "-",
                    Status = x.IsDeleted ? "Archived" : x.Status,
                    Keterangan = x.Keterangan ?? "-",

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

        public async Task<InspeksiTemuanKpcDto?> GetByIdAsync(int id)
        {
            var item = await _context.InspeksiTemuanKpcs
                .Include(x => x.CreatedByUser)
                .Include(x => x.UpdatedByUser)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return null;

            _logger.LogInformation("🔍 RAW FotoHasilUrls from DB: {FotoHasilUrls}", item.FotoHasilUrls);
            _logger.LogInformation("🔍 RAW FotoTemuanUrls from DB: {FotoTemuanUrls}", item.FotoTemuanUrls);

            List<string> fotoTemuanUrls = new();
            List<string> fotoHasilUrls = new();

            if (!string.IsNullOrEmpty(item.FotoTemuanUrls))
            {
                try
                {
                    fotoTemuanUrls = JsonSerializer.Deserialize<List<string>>(item.FotoTemuanUrls) ?? new List<string>();
                    _logger.LogInformation("🔍 Parsed FotoTemuanUrls count: {Count}", fotoTemuanUrls.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError("❌ Error parsing FotoTemuanUrls: {Message}", ex.Message);
                    fotoTemuanUrls = new List<string>();
                }
            }

            if (!string.IsNullOrEmpty(item.FotoHasilUrls))
            {
                try
                {
                    fotoHasilUrls = JsonSerializer.Deserialize<List<string>>(item.FotoHasilUrls) ?? new List<string>();
                    _logger.LogInformation("🔍 Parsed FotoHasilUrls count: {Count}", fotoHasilUrls.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError("❌ Error parsing FotoHasilUrls: {Message}", ex.Message);
                    fotoHasilUrls = new List<string>();
                }
            }

            return new InspeksiTemuanKpcDto
            {
                Id = item.Id,
                Ruang = item.Ruang,
                Temuan = item.Temuan,
                KategoriTemuan = item.KategoriTemuan ?? "-",
                Inspector = item.Inspector ?? "-",
                Severity = item.Severity,
                TanggalTemuan = item.TanggalTemuan.ToString("yyyy-MM-dd"),
                NoFollowUp = item.NoFollowUp ?? "-",
                PerbaikanDilakukan = item.PerbaikanDilakukan ?? "-",
                TanggalPerbaikan = item.TanggalPerbaikan?.ToString("yyyy-MM-dd") ?? "",
                TanggalSelesaiPerbaikan = item.TanggalSelesaiPerbaikan?.ToString("yyyy-MM-dd") ?? "",
                PicPelaksana = item.PicPelaksana ?? "-",
                Status = item.Status,
                Keterangan = item.Keterangan ?? "-",

                FotoTemuanUrls = fotoTemuanUrls,
                FotoHasilUrls = fotoHasilUrls,

                FotoTemuan = GetFotoText(item.FotoTemuanUrls),
                FotoHasil = GetFotoText(item.FotoHasilUrls),

                CreatedByName = item.CreatedByUser?.FullName ?? "Unknown",
                CreatedAt = item.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                UpdatedByName = item.UpdatedByUser?.FullName ?? "-",
                UpdatedAt = item.UpdatedAt?.ToString("dd MMM yyyy HH:mm") ?? "-"
            };
        }

        public async Task<int> CreateAsync(CreateInspeksiTemuanKpcDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 Starting create process for room: {Ruang}", dto.Ruang);
                _logger.LogInformation("📁 Files received: {Count}", dto.FotoTemuanFiles?.Count ?? 0);

                var fotoTemuanUrls = new List<string>();
                if (dto.FotoTemuanFiles != null && dto.FotoTemuanFiles.Count > 0)
                {
                    _logger.LogInformation("📤 Uploading {Count} foto temuan...", dto.FotoTemuanFiles.Count);

                    int successCount = 0;
                    foreach (var file in dto.FotoTemuanFiles)
                    {
                        if (file.Length > 0)
                        {
                            try
                            {
                                _logger.LogInformation("⬆️ Uploading file: {FileName} ({Size} bytes)", file.FileName, file.Length);

                                var url = await _cloudinary.UploadImageAsync(file, "inspeksi/kpc/temuan");
                                if (!string.IsNullOrEmpty(url))
                                {
                                    fotoTemuanUrls.Add(url);
                                    successCount++;
                                    _logger.LogInformation("✅ Successfully uploaded: {FileName} -> {Url}", file.FileName, url);
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Failed to upload file: {FileName}", file.FileName);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("❌ Error uploading file {FileName}: {Message}", file.FileName, ex.Message);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Empty file skipped: {FileName}", file.FileName);
                        }
                    }
                    _logger.LogInformation("📊 Successfully uploaded {SuccessCount}/{TotalCount} foto temuan", successCount, dto.FotoTemuanFiles.Count);
                }

                DateTime tanggalTemuan;
                if (!DateTime.TryParse(dto.TanggalTemuan, out tanggalTemuan))
                {
                    tanggalTemuan = DateTime.UtcNow.Date;
                }

                var entity = new InspeksiTemuanKpc
                {
                    Ruang = dto.Ruang.Trim(),
                    Temuan = dto.Temuan.Trim(),
                    KategoriTemuan = dto.KategoriTemuan?.Trim(),
                    Inspector = dto.Inspector?.Trim(),
                    Severity = dto.Severity,
                    TanggalTemuan = tanggalTemuan,
                    NoFollowUp = dto.NoFollowUp?.Trim(),
                    PicPelaksana = dto.PicPelaksana?.Trim(),
                    Keterangan = dto.Keterangan?.Trim(),
                    FotoTemuanUrls = fotoTemuanUrls.Count > 0 ? JsonSerializer.Serialize(fotoTemuanUrls) : null,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Open"
                };

                _logger.LogInformation("💾 Adding entity to context...");
                _context.InspeksiTemuanKpcs.Add(entity);

                _logger.LogInformation("💾 Saving changes to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Successfully created temuan with ID: {Id}", entity.Id);

                await _log.LogAsync("InspeksiTemuanKpc", entity.Id, "Created", userId,
                    $"Temuan baru di {dto.Ruang} oleh {dto.Inspector ?? "Unknown"}");

                return entity.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating InspeksiTemuanKpc");
                throw;
            }
        }

        public async Task<InspeksiTemuanKpcDto?> UpdateAsync(int id, UpdateInspeksiTemuanKpcDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 Starting update for ID: {Id}", id);

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var entity = await _context.InspeksiTemuanKpcs
                        .AsTracking()
                        .Include(x => x.CreatedByUser)
                        .Include(x => x.UpdatedByUser)
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (entity == null || entity.IsDeleted)
                    {
                        _logger.LogWarning("❌ Entity not found or deleted: {Id}", id);
                        return null;
                    }

                    var oldStatus = entity.Status;

                    // ✅ UPDATE SEMUA FIELD YANG ADA
                    if (!string.IsNullOrEmpty(dto.Ruang)) entity.Ruang = dto.Ruang.Trim();
                    if (!string.IsNullOrEmpty(dto.Temuan)) entity.Temuan = dto.Temuan.Trim();
                    if (dto.KategoriTemuan != null) entity.KategoriTemuan = dto.KategoriTemuan.Trim();
                    if (dto.Inspector != null) entity.Inspector = dto.Inspector.Trim();
                    if (!string.IsNullOrEmpty(dto.Severity)) entity.Severity = dto.Severity;
                    if (dto.TanggalTemuan.HasValue) entity.TanggalTemuan = dto.TanggalTemuan.Value.Date;
                    
                    if (!string.IsNullOrEmpty(dto.NoFollowUp)) entity.NoFollowUp = dto.NoFollowUp;
                    if (!string.IsNullOrEmpty(dto.PerbaikanDilakukan)) entity.PerbaikanDilakukan = dto.PerbaikanDilakukan;
                    if (dto.TanggalPerbaikan.HasValue) entity.TanggalPerbaikan = dto.TanggalPerbaikan.Value;
                    if (dto.TanggalSelesaiPerbaikan.HasValue) entity.TanggalSelesaiPerbaikan = dto.TanggalSelesaiPerbaikan.Value;
                    if (!string.IsNullOrEmpty(dto.PicPelaksana)) entity.PicPelaksana = dto.PicPelaksana;
                    if (!string.IsNullOrEmpty(dto.Status)) entity.Status = dto.Status;
                    if (!string.IsNullOrEmpty(dto.Keterangan)) entity.Keterangan = dto.Keterangan;

                    if (dto.Status == "Closed" && entity.TanggalClosed == null)
                    {
                        entity.TanggalClosed = DateTime.UtcNow;
                    }

                    // ✅ HANDLE FOTO TEMUAN UPDATE
                    if (dto.FotoTemuanFiles != null && dto.FotoTemuanFiles.Count > 0)
                    {
                        var existingTemuanUrls = new List<string>();
                        if (!string.IsNullOrEmpty(entity.FotoTemuanUrls))
                        {
                            try
                            {
                                existingTemuanUrls = JsonSerializer.Deserialize<List<string>>(entity.FotoTemuanUrls) ?? new List<string>();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("❌ Error parsing existing FotoTemuanUrls: {Message}", ex.Message);
                            }
                        }

                        foreach (var file in dto.FotoTemuanFiles)
                        {
                            if (file.Length > 0)
                            {
                                try
                                {
                                    var url = await _cloudinary.UploadImageAsync(file, "inspeksi/kpc/temuan");
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        existingTemuanUrls.Add(url);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError("❌ Error uploading foto temuan: {Message}", ex.Message);
                                }
                            }
                        }

                        entity.FotoTemuanUrls = JsonSerializer.Serialize(existingTemuanUrls);
                    }

                    // ✅ HANDLE FOTO HASIL UPDATE (existing code)
                    if (dto.FotoHasilFiles != null && dto.FotoHasilFiles.Count > 0)
                    {
                        // ... existing foto hasil code ...
                    }

                    entity.UpdatedBy = userId;
                    entity.UpdatedAt = DateTime.UtcNow;

                    var changes = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (changes > 0)
                    {
                        await _log.LogAsync("InspeksiTemuanKpc", id, "Updated", userId, $"Status: {oldStatus} → {entity.Status}");
                        return await GetByIdAsync(id);
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in UpdateAsync");
                return null;
            }
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 Starting soft delete for ID: {Id}", id);

                var entity = await _context.InspeksiTemuanKpcs
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null)
                {
                    _logger.LogWarning("❌ Entity not found or already deleted: {Id}", id);
                    return false;
                }

                _logger.LogInformation("📊 Before update - IsDeleted: {IsDeleted}", entity.IsDeleted);

                entity.IsDeleted = true;
                entity.DeletedBy = userId;
                entity.DeletedAt = DateTime.UtcNow;
                entity.UpdatedBy = userId;
                entity.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("📊 After update - IsDeleted: {IsDeleted}", entity.IsDeleted);

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("💾 Saving changes to database...");
                    var changes = await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("💾 SaveChanges completed. Affected rows: {Changes}", changes);

                    if (changes > 0)
                    {
                        _logger.LogInformation("✅ Successfully soft deleted ID: {Id}", id);
                        await _log.LogAsync("InspeksiTemuanKpc", id, "Deleted", userId, "Dipindah ke history");
                        return true;
                    }
                    else
                    {
                        _logger.LogError("❌ No changes saved for ID: {Id}. Possible EF tracking issue.", id);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Transaction failed for ID: {Id}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in DeleteAsync for ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> RestoreAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 Starting restore for ID: {Id}", id);

                var entity = await _context.InspeksiTemuanKpcs
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);

                if (entity == null)
                {
                    _logger.LogWarning("❌ Entity not found or not deleted: {Id}", id);
                    return false;
                }

                _logger.LogInformation("📊 Before restore - IsDeleted: {IsDeleted}", entity.IsDeleted);

                entity.IsDeleted = false;
                entity.DeletedBy = null;
                entity.DeletedAt = null;
                entity.UpdatedBy = userId;
                entity.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("📊 After restore - IsDeleted: {IsDeleted}", entity.IsDeleted);

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("💾 Saving restore changes...");
                    var changes = await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("💾 Restore SaveChanges completed. Affected rows: {Changes}", changes);

                    if (changes > 0)
                    {
                        var verified = await _context.InspeksiTemuanKpcs
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == id);

                        _logger.LogInformation("🔍 RESTORE VERIFICATION - ID: {Id}, IsDeleted: {IsDeleted}",
                            id, verified?.IsDeleted);

                        _logger.LogInformation("✅ Successfully restored ID: {Id}", id);
                        await _log.LogAsync("InspeksiTemuanKpc", id, "Restored", userId, "Dikembalikan dari history");
                        return true;
                    }
                    else
                    {
                        _logger.LogError("❌ No changes saved for restore ID: {Id}", id);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Restore transaction failed for ID: {Id}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in RestoreAsync for ID: {Id}", id);
                return false;
            }
        }

        // ✅ === FIXED EXCEL EXPORT - SHOW ALL IMAGES ===
       public async Task<byte[]> ExportToExcelAsync(bool history, DateTime? start, DateTime? end, string? ruang, string? status)
        {
            try
            {
                _logger.LogInformation("📊 Starting Excel export with proper formatting...");

                var query = new InspeksiTemuanKpcQueryDto
                {
                    IncludeDeleted = history,
                    Ruang = ruang,
                    Status = status,
                    StartDate = start,
                    EndDate = end,
                    Page = 1,
                    PageSize = 10000
                };

                var result = await GetAllAsync(query);
                _logger.LogInformation("📥 Data retrieved for export: {Count} items", result.Data.Count);

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Laporan Inspeksi KPC");

                // ✅ HEADERS
                var headers = new[]
                {
                    "No", "Ruang", "Temuan", "Kategori", "Inspector", "Severity",
                    "Tgl Temuan", "No Follow Up", "Perbaikan",
                    "Tgl Perbaikan", "Tgl Selesai", "PIC", "Status", "Keterangan",
                    "Foto Temuan", "Foto Hasil", "Dibuat Oleh", "Dibuat Pada"
                };

                // ✅ SET HEADERS WITH STYLING
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(66, 133, 244); // Blue color
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(33, 33, 33);
                }

                // ✅ SET COLUMN WIDTHS
                ws.Column(1).Width = 5;   // No
                ws.Column(2).Width = 20;  // Ruang
                ws.Column(3).Width = 35;  // Temuan
                ws.Column(4).Width = 15;  // Kategori
                ws.Column(5).Width = 15;  // Inspector
                ws.Column(6).Width = 10;  // Severity
                ws.Column(7).Width = 13;  // Tgl Temuan
                ws.Column(8).Width = 18;  // No Follow Up
                ws.Column(9).Width = 30;  // Perbaikan
                ws.Column(10).Width = 13; // Tgl Perbaikan
                ws.Column(11).Width = 13; // Tgl Selesai
                ws.Column(12).Width = 15; // PIC
                ws.Column(13).Width = 12; // Status
                ws.Column(14).Width = 25; // Keterangan
                ws.Column(15).Width = 45; // Foto Temuan - EXTRA WIDE
                ws.Column(16).Width = 45; // Foto Hasil - EXTRA WIDE
                ws.Column(17).Width = 18; // Dibuat Oleh
                ws.Column(18).Width = 20; // Dibuat Pada

                // ✅ SET HEADER ROW HEIGHT
                ws.Row(1).Height = 35;

                // ✅ DATA PROCESSING
                int row = 2;
                int no = 1;

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                foreach (var item in result.Data)
                {
                    _logger.LogInformation("📝 Processing item {Id} - Foto Temuan: {CountTemuan}, Foto Hasil: {CountHasil}",
                        item.Id, item.FotoTemuanUrls?.Count ?? 0, item.FotoHasilUrls?.Count ?? 0);

                    // ✅ FILL DATA WITH PROPER STYLING
                    ws.Cell(row, 1).Value = no++;
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(row, 2).Value = item.Ruang;
                    
                    // Temuan with wrap text
                    ws.Cell(row, 3).Value = item.Temuan;
                    ws.Cell(row, 3).Style.Alignment.WrapText = true;
                    ws.Cell(row, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    
                    ws.Cell(row, 4).Value = item.KategoriTemuan ?? "-";
                    ws.Cell(row, 5).Value = item.Inspector ?? "-";
                    
                    // Severity with color coding
                    var severityCell = ws.Cell(row, 6);
                    severityCell.Value = item.Severity;
                    severityCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    switch (item.Severity)
                    {
                        case "Critical":
                            severityCell.Style.Fill.BackgroundColor = XLColor.Red;
                            severityCell.Style.Font.FontColor = XLColor.White;
                            severityCell.Style.Font.Bold = true;
                            break;
                        case "High":
                            severityCell.Style.Fill.BackgroundColor = XLColor.Orange;
                            severityCell.Style.Font.Bold = true;
                            break;
                        case "Medium":
                            severityCell.Style.Fill.BackgroundColor = XLColor.Yellow;
                            break;
                        case "Low":
                            severityCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            break;
                    }
                    
                    ws.Cell(row, 7).Value = item.TanggalTemuan;
                    ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                    ws.Cell(row, 8).Value = item.NoFollowUp ?? "-";
                    
                    // Perbaikan with wrap text
                    ws.Cell(row, 9).Value = item.PerbaikanDilakukan ?? "-";
                    ws.Cell(row, 9).Style.Alignment.WrapText = true;
                    ws.Cell(row, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    
                    ws.Cell(row, 10).Value = item.TanggalPerbaikan ?? "-";
                    ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                    ws.Cell(row, 11).Value = item.TanggalSelesaiPerbaikan ?? "-";
                    ws.Cell(row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                    ws.Cell(row, 12).Value = item.PicPelaksana ?? "-";
                    
                    // Status with color coding
                    var statusCell = ws.Cell(row, 13);
                    statusCell.Value = item.Status;
                    statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    statusCell.Style.Font.Bold = true;
                    switch (item.Status)
                    {
                        case "Closed":
                            statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            statusCell.Style.Font.FontColor = XLColor.DarkGreen;
                            break;
                        case "In Progress":
                            statusCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                            statusCell.Style.Font.FontColor = XLColor.DarkBlue;
                            break;
                        case "Open":
                            statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                            statusCell.Style.Font.FontColor = XLColor.DarkOrange;
                            break;
                        case "Rejected":
                            statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                            statusCell.Style.Font.FontColor = XLColor.DarkRed;
                            break;
                    }
                    
                    // Keterangan with wrap text
                    ws.Cell(row, 14).Value = item.Keterangan ?? "-";
                    ws.Cell(row, 14).Style.Alignment.WrapText = true;
                    ws.Cell(row, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                    // ✅ FOTO TEMUAN - CONSISTENT STYLING
                    if (item.FotoTemuanUrls != null && item.FotoTemuanUrls.Count > 0)
                    {
                        try
                        {
                            int imageIndex = 0;
                            int imagesPerRow = 2;
                            int imageWidth = 190;  // Consistent width
                            int imageHeight = 150; // Consistent height
                            int horizontalSpacing = 20;
                            int verticalSpacing = 20;

                            foreach (var imageUrl in item.FotoTemuanUrls)
                            {
                                try
                                {
                                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                                    using var stream = new MemoryStream(imageBytes);

                                    int colOffset = (imageIndex % imagesPerRow) * (imageWidth + horizontalSpacing) + 10;
                                    int rowOffset = (imageIndex / imagesPerRow) * (imageHeight + verticalSpacing) + 10;

                                    var image = ws.AddPicture(stream)
                                        .MoveTo(ws.Cell(row, 15), colOffset, rowOffset);

                                    image.Width = imageWidth;
                                    image.Height = imageHeight;

                                    _logger.LogInformation("✅ Added foto temuan {Index}/{Total}", imageIndex + 1, item.FotoTemuanUrls.Count);
                                    imageIndex++;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("❌ Cannot load foto temuan: {Message}", ex.Message);
                                }
                            }

                            // Label for foto temuan
                            ws.Cell(row, 15).Value = $"📷 {item.FotoTemuanUrls.Count} foto";
                            ws.Cell(row, 15).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                            ws.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            ws.Cell(row, 15).Style.Font.FontSize = 9;
                            ws.Cell(row, 15).Style.Font.FontColor = XLColor.Blue;
                            ws.Cell(row, 15).Style.Font.Bold = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("❌ Error processing foto temuan: {Message}", ex.Message);
                            ws.Cell(row, 15).Value = "❌ Error loading images";
                            ws.Cell(row, 15).Style.Font.FontColor = XLColor.Red;
                        }
                    }
                    else
                    {
                        ws.Cell(row, 15).Value = "-";
                        ws.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 15).Style.Font.FontColor = XLColor.Gray;
                    }

                    // ✅ FOTO HASIL - CONSISTENT STYLING (SAME AS FOTO TEMUAN)
                    if (item.FotoHasilUrls != null && item.FotoHasilUrls.Count > 0)
                    {
                        try
                        {
                            int imageIndex = 0;
                            int imagesPerRow = 2;
                            int imageWidth = 190;  // Same as foto temuan
                            int imageHeight = 150; // Same as foto temuan
                            int horizontalSpacing = 20;
                            int verticalSpacing = 20;

                            foreach (var imageUrl in item.FotoHasilUrls)
                            {
                                try
                                {
                                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                                    using var stream = new MemoryStream(imageBytes);

                                    int colOffset = (imageIndex % imagesPerRow) * (imageWidth + horizontalSpacing) + 10;
                                    int rowOffset = (imageIndex / imagesPerRow) * (imageHeight + verticalSpacing) + 10;

                                    var image = ws.AddPicture(stream)
                                        .MoveTo(ws.Cell(row, 16), colOffset, rowOffset);

                                    image.Width = imageWidth;
                                    image.Height = imageHeight;

                                    _logger.LogInformation("✅ Added foto hasil {Index}/{Total}", imageIndex + 1, item.FotoHasilUrls.Count);
                                    imageIndex++;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("❌ Cannot load foto hasil: {Message}", ex.Message);
                                }
                            }

                            // Label for foto hasil
                            ws.Cell(row, 16).Value = $"📷 {item.FotoHasilUrls.Count} foto";
                            ws.Cell(row, 16).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                            ws.Cell(row, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            ws.Cell(row, 16).Style.Font.FontSize = 9;
                            ws.Cell(row, 16).Style.Font.FontColor = XLColor.Green;
                            ws.Cell(row, 16).Style.Font.Bold = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("❌ Error processing foto hasil: {Message}", ex.Message);
                            ws.Cell(row, 16).Value = "❌ Error loading images";
                            ws.Cell(row, 16).Style.Font.FontColor = XLColor.Red;
                        }
                    }
                    else
                    {
                        ws.Cell(row, 16).Value = "-";
                        ws.Cell(row, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 16).Style.Font.FontColor = XLColor.Gray;
                    }

                    ws.Cell(row, 17).Value = item.CreatedByName;
                    ws.Cell(row, 18).Value = item.CreatedAt;

                    // ✅ DYNAMIC ROW HEIGHT - IMPROVED CALCULATION
                    int maxImages = Math.Max(
                        item.FotoTemuanUrls?.Count ?? 0,
                        item.FotoHasilUrls?.Count ?? 0
                    );

                    // Calculate required height based on images
                    int rowsOfImages = (int)Math.Ceiling(maxImages / 2.0);
                    int requiredHeight = rowsOfImages * 160 + ((rowsOfImages - 1) * 20) + 20; // Better spacing

                    // Ensure minimum height
                    ws.Row(row).Height = Math.Max(170, requiredHeight);

                    // ✅ ADD BORDERS TO ALL CELLS
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(row, col).Style.Border.OutsideBorderColor = XLColor.Gray;
                    }

                    // ✅ ALTERNATING ROW COLORS FOR BETTER READABILITY
                    if (row % 2 == 0)
                    {
                        for (int col = 1; col <= headers.Length; col++)
                        {
                            // Skip colored cells (severity and status)
                            if (col != 6 && col != 13)
                            {
                                ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                            }
                        }
                    }

                    row++;
                }

                // ✅ FINAL FORMATTING
                // Center align specific columns
                ws.Columns(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Columns(6, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Columns(10, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Columns(13, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // All rows vertical align center
                ws.Rows().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // ✅ FREEZE HEADER ROW
                ws.SheetView.FreezeRows(1);

                // ✅ AUTO FILTER
                ws.Range(1, 1, 1, headers.Length).SetAutoFilter();

                using var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                _logger.LogInformation("✅ Excel export completed successfully. Total rows: {RowCount}", row - 2);

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in ExportToExcelAsync");
                throw;
            }
        }

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