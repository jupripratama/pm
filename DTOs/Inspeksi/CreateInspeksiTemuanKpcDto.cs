// DTOs/CreateInspeksiTemuanKpcDto.cs - CREATE DTO
using Microsoft.AspNetCore.Http;

namespace Pm.DTOs
{
    public class CreateInspeksiTemuanKpcDto
    {
        public required string Ruang { get; set; }
        public required string Temuan { get; set; }
        public string? KategoriTemuan { get; set; }
        public string? Inspector { get; set; }
        public string Severity { get; set; } = "Medium";
        public DateTime TanggalTemuan { get; set; } = DateTime.Today;
        public string? NoFollowUp { get; set; }
        public string? PicPelaksana { get; set; }
        public string? Keterangan { get; set; }
        public List<IFormFile>? FotoTemuanFiles { get; set; }
    }
}