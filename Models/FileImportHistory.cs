// Tambahkan model baru di Models folder
using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class FileImportHistory
    {
        [Key]
        public int ImportHistoryId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        public DateTime ImportDate { get; set; } = DateTime.UtcNow;
        
        public int RecordCount { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Failed
        
        public string? ErrorMessage { get; set; }
    }
}