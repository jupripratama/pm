// DTOs/InspeksiTemuanKpcDto.cs - RESPONSE DTO
namespace Pm.DTOs
{
    public class InspeksiTemuanKpcDto
    {
        public int Id { get; set; }
        public string Ruang { get; set; } = "";
        public string Temuan { get; set; } = "";
        public string KategoriTemuan { get; set; } = "-";
        public string Inspector { get; set; } = "-";
        public string Severity { get; set; } = "";
        public string TanggalTemuan { get; set; } = "";
        public string NoFollowUp { get; set; } = "-";
        public string FollowUpRef { get; set; } = "-";
        public string PerbaikanDilakukan { get; set; } = "-";
        public string TanggalPerbaikan { get; set; } = "-";
        public string TanggalSelesaiPerbaikan { get; set; } = "-";
        public string PicPelaksana { get; set; } = "-";
        public string Status { get; set; } = "Open";
        public string Keterangan { get; set; } = "-";

        // Arrays of image URLs
        public List<string> FotoTemuanUrls { get; set; } = new();
        public List<string> FotoHasilUrls { get; set; } = new();

        // Display text
        public string FotoTemuan { get; set; } = "-";
        public string FotoHasil { get; set; } = "-";

        public string CreatedByName { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string? UpdatedByName { get; set; }
        public string? UpdatedAt { get; set; }
    }
}