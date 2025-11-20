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

                    _logger.LogInformation("📁 Files received: {Count}", dto.FotoHasilFiles?.Count ?? 0);

                    var oldStatus = entity.Status;

                    if (!string.IsNullOrEmpty(dto.NoFollowUp))
                        entity.NoFollowUp = dto.NoFollowUp;
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

                    if (dto.FotoHasilFiles != null && dto.FotoHasilFiles.Count > 0)
                    {
                        _logger.LogInformation("📤 Uploading {Count} foto hasil for ID {Id}...", dto.FotoHasilFiles.Count, id);

                        var existingUrls = new List<string>();

                        if (!string.IsNullOrEmpty(entity.FotoHasilUrls))
                        {
                            try
                            {
                                existingUrls = JsonSerializer.Deserialize<List<string>>(entity.FotoHasilUrls) ?? new List<string>();
                                _logger.LogInformation("📷 Existing foto hasil count: {Count}", existingUrls.Count);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("❌ Error parsing existing FotoHasilUrls: {Message}", ex.Message);
                            }
                        }

                        int successCount = 0;
                        foreach (var file in dto.FotoHasilFiles)
                        {
                            if (file.Length > 0)
                            {
                                try
                                {
                                    _logger.LogInformation("⬆️ Uploading file: {FileName}", file.FileName);
                                    var url = await _cloudinary.UploadImageAsync(file, "inspeksi/kpc/hasil");
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        existingUrls.Add(url);
                                        successCount++;
                                        _logger.LogInformation("✅ Successfully uploaded foto hasil: {FileName}", file.FileName);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError("❌ Error uploading file {FileName}: {Message}", file.FileName, ex.Message);
                                }
                            }
                        }

                        entity.FotoHasilUrls = JsonSerializer.Serialize(existingUrls);
                        _logger.LogInformation("💾 Total foto hasil after update: {Count}", existingUrls.Count);
                    }

                    entity.UpdatedBy = userId;
                    entity.UpdatedAt = DateTime.UtcNow;

                    var changes = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("💾 SaveChanges completed. Affected rows: {Changes}", changes);

                    if (changes > 0)
                    {
                        await _log.LogAsync("InspeksiTemuanKpc", id, "Updated", userId, $"Status: {oldStatus} → {entity.Status}");
                        _logger.LogInformation("✅ Successfully updated temuan ID: {Id}", id);

                        return await GetByIdAsync(id);
                    }
                    else
                    {
                        _logger.LogError("❌ SaveChanges returned 0 affected rows even with AsTracking()!");
                        await transaction.RollbackAsync();
                        return null;
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
                _logger.LogError(ex, "❌ UNEXPECTED ERROR in UpdateAsync for ID: {Id}", id);
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
                _logger.LogInformation("📊 Starting Excel export with ALL images...");

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

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                }

                // Style header
                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRow.Height = 25;

                // Atur lebar kolom
                ws.Column(15).Width = 30; // Foto Temuan - LEBIH LEBAR UNTUK BANYAK GAMBAR
                ws.Column(16).Width = 30; // Foto Hasil - LEBIH LEBAR UNTUK BANYAK GAMBAR
                ws.Column(2).Width = 15;  // Ruang
                ws.Column(3).Width = 30;  // Temuan
                ws.Column(8).Width = 15;  // No Follow Up
                ws.Column(9).Width = 25;  // Perbaikan

                // Data
                int row = 2;
                int no = 1;

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                foreach (var item in result.Data)
                {
                    _logger.LogInformation("📝 Processing item {Id} for export - Foto Temuan: {CountTemuan}, Foto Hasil: {CountHasil}",
                        item.Id, item.FotoTemuanUrls?.Count ?? 0, item.FotoHasilUrls?.Count ?? 0);

                    // ✅ DATA
                    ws.Cell(row, 1).Value = no++;
                    ws.Cell(row, 2).Value = item.Ruang;
                    ws.Cell(row, 3).Value = item.Temuan;
                    ws.Cell(row, 4).Value = item.KategoriTemuan ?? "-";
                    ws.Cell(row, 5).Value = item.Inspector ?? "-";
                    ws.Cell(row, 6).Value = item.Severity;
                    ws.Cell(row, 7).Value = item.TanggalTemuan;
                    ws.Cell(row, 8).Value = item.NoFollowUp ?? "-";
                    ws.Cell(row, 9).Value = item.PerbaikanDilakukan ?? "-";
                    ws.Cell(row, 10).Value = item.TanggalPerbaikan ?? "-";
                    ws.Cell(row, 11).Value = item.TanggalSelesaiPerbaikan ?? "-";
                    ws.Cell(row, 12).Value = item.PicPelaksana ?? "-";
                    ws.Cell(row, 13).Value = item.Status;
                    ws.Cell(row, 14).Value = item.Keterangan ?? "-";

                    // ✅ FOTO TEMUAN - TAMPILKAN SEMUA GAMBAR
                    if (item.FotoTemuanUrls != null && item.FotoTemuanUrls.Count > 0)
                    {
                        try
                        {
                            _logger.LogInformation("📸 Adding {Count} foto temuan for item {Id}", item.FotoTemuanUrls.Count, item.Id);

                            int imageIndex = 0;
                            int imagesPerRow = 2; // 2 gambar per baris
                            int imageWidth = 140;
                            int imageHeight = 110;
                            int horizontalSpacing = 10;
                            int verticalSpacing = 10;

                            foreach (var imageUrl in item.FotoTemuanUrls)
                            {
                                try
                                {
                                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                                    using var stream = new MemoryStream(imageBytes);

                                    // Hitung posisi gambar (grid layout)
                                    int colOffset = (imageIndex % imagesPerRow) * (imageWidth + horizontalSpacing);
                                    int rowOffset = (imageIndex / imagesPerRow) * (imageHeight + verticalSpacing);

                                    var image = ws.AddPicture(stream)
                                        .MoveTo(ws.Cell(row, 15), colOffset, rowOffset);

                                    image.Width = imageWidth;
                                    image.Height = imageHeight;

                                    _logger.LogInformation("✅ Added foto temuan {Index}/{Total}", imageIndex + 1, item.FotoTemuanUrls.Count);
                                    imageIndex++;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("❌ Cannot load foto temuan from {Url}: {Message}", imageUrl, ex.Message);
                                }
                            }

                            // Set label
                            ws.Cell(row, 15).Value = $"({item.FotoTemuanUrls.Count} foto)";
                            ws.Cell(row, 15).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                            ws.Cell(row, 15).Style.Font.FontSize = 9;
                            ws.Cell(row, 15).Style.Font.FontColor = XLColor.Blue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("❌ Error processing foto temuan: {Message}", ex.Message);
                            ws.Cell(row, 15).Value = $"❌ Error ({item.FotoTemuanUrls.Count} foto)";
                        }
                    }
                    else
                    {
                        ws.Cell(row, 15).Value = "-";
                    }

                    // ✅ FOTO HASIL - TAMPILKAN SEMUA GAMBAR
                    if (item.FotoHasilUrls != null && item.FotoHasilUrls.Count > 0)
                    {
                        try
                        {
                            _logger.LogInformation("📸 Adding {Count} foto hasil for item {Id}", item.FotoHasilUrls.Count, item.Id);

                            int imageIndex = 0;
                            int imagesPerRow = 2; // 2 gambar per baris
                            int imageWidth = 140;
                            int imageHeight = 110;
                            int horizontalSpacing = 10;
                            int verticalSpacing = 10;

                            foreach (var imageUrl in item.FotoHasilUrls)
                            {
                                try
                                {
                                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                                    using var stream = new MemoryStream(imageBytes);

                                    // Hitung posisi gambar (grid layout)
                                    int colOffset = (imageIndex % imagesPerRow) * (imageWidth + horizontalSpacing);
                                    int rowOffset = (imageIndex / imagesPerRow) * (imageHeight + verticalSpacing);

                                    var image = ws.AddPicture(stream)
                                        .MoveTo(ws.Cell(row, 16), colOffset, rowOffset);

                                    image.Width = imageWidth;
                                    image.Height = imageHeight;

                                    _logger.LogInformation("✅ Added foto hasil {Index}/{Total}", imageIndex + 1, item.FotoHasilUrls.Count);
                                    imageIndex++;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("❌ Cannot load foto hasil from {Url}: {Message}", imageUrl, ex.Message);
                                }
                            }

                            // Set label
                            ws.Cell(row, 16).Value = $"({item.FotoHasilUrls.Count} foto)";
                            ws.Cell(row, 16).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                            ws.Cell(row, 16).Style.Font.FontSize = 9;
                            ws.Cell(row, 16).Style.Font.FontColor = XLColor.Green;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("❌ Error processing foto hasil: {Message}", ex.Message);
                            ws.Cell(row, 16).Value = $"❌ Error ({item.FotoHasilUrls.Count} foto)";
                        }
                    }
                    else
                    {
                        ws.Cell(row, 16).Value = "-";
                    }

                    ws.Cell(row, 17).Value = item.CreatedByName;
                    ws.Cell(row, 18).Value = item.CreatedAt;

                    // ✅ DYNAMIC ROW HEIGHT - SESUAIKAN DENGAN JUMLAH GAMBAR
                    int maxImages = Math.Max(
                        item.FotoTemuanUrls?.Count ?? 0,
                        item.FotoHasilUrls?.Count ?? 0
                    );

                    // Hitung tinggi yang dibutuhkan (2 gambar per baris)
                    int rowsOfImages = (int)Math.Ceiling(maxImages / 2.0);
                    int requiredHeight = rowsOfImages * 120 + ((rowsOfImages - 1) * 10); // 120 per gambar + spacing

                    ws.Row(row).Height = Math.Max(125, requiredHeight);

                    row++;
                }

                // Style tambahan
                ws.Rows().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Columns().AdjustToContents();

                using var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                _logger.LogInformation("✅ Excel export completed successfully with ALL images. Total rows: {RowCount}", row - 2);

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