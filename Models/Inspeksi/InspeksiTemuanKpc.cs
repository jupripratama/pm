using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [MaxLength(200)]
        public string? Inspector { get; set; }

        public string Severity { get; set; } = "Medium";

        public DateTime TanggalTemuan { get; set; } = DateTime.UtcNow;

        public string? NoFollowUp { get; set; }

        public string? PerbaikanDilakukan { get; set; } = string.Empty;
        public DateTime? TanggalPerbaikan { get; set; }
        public DateTime? TanggalSelesaiPerbaikan { get; set; }
        public string? PicPelaksana { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime? TanggalTargetSelesai { get; set; }
        public DateTime? TanggalClosed { get; set; }
        public string? Keterangan { get; set; }

        // Foto disimpan sebagai JSON array URL dari Cloudinary
        public string? FotoTemuanUrls { get; set; }
        public string? FotoHasilUrls { get; set; }

        // Audit Trail - HANYA SIMPAN ID SAJA
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation Properties - HANYA UNTIK QUERY
        [ForeignKey("CreatedBy")]
        public virtual User CreatedByUser { get; set; } = null!;

        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }

        [ForeignKey("DeletedBy")]
        public virtual User? DeletedByUser { get; set; }
    }
}