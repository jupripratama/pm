// Models/InspeksiTemuanKpc.cs - COMPLETE VERSION
using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class InspeksiTemuanKpc
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Ruang { get; set; } = string.Empty;

        [Required]
        public string Temuan { get; set; } = string.Empty;

        public string? KategoriTemuan { get; set; }

        // ✅ TAMBAHAN: Siapa yang melakukan inspeksi
        [MaxLength(200)]
        public string? Inspector { get; set; }

        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

        public DateTime TanggalTemuan { get; set; } = DateTime.UtcNow;

        public string? NoFollowUp { get; set; }          // WR-2025-001 atau email
        public string? FollowUpRef { get; set; }          // email atau nomor WR

        public string? PerbaikanDilakukan { get; set; } = string.Empty;
        public DateTime? TanggalPerbaikan { get; set; }

        // ✅ TAMBAHAN: Tanggal selesai perbaikan
        public DateTime? TanggalSelesaiPerbaikan { get; set; }

        public string? PicPelaksana { get; set; }

        public string Status { get; set; } = "Open"; // Open, In Progress, Closed, Rejected

        public DateTime? TanggalTargetSelesai { get; set; }
        public DateTime? TanggalClosed { get; set; }

        public string? Keterangan { get; set; }

        // Foto disimpan sebagai JSON array URL dari Cloudinary
        public string? FotoTemuanUrls { get; set; }
        public string? FotoHasilUrls { get; set; }

        // Audit Trail
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual User CreatedByUser { get; set; } = null!;

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual User? UpdatedByUser { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
        public virtual User? DeletedByUser { get; set; }
    }
}