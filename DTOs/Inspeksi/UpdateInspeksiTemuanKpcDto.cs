// DTOs/UpdateInspeksiTemuanKpcDto.cs - UPDATE DTO
using Microsoft.AspNetCore.Http;

namespace Pm.DTOs
{
    public class UpdateInspeksiTemuanKpcDto
    {
        public string? FollowUpRef { get; set; }
        public string? PerbaikanDilakukan { get; set; }
        public DateTime? TanggalPerbaikan { get; set; }
        public DateTime? TanggalSelesaiPerbaikan { get; set; }
        public string? PicPelaksana { get; set; }
        public string? Status { get; set; }
        public string? Keterangan { get; set; }
        public List<IFormFile>? FotoHasilFiles { get; set; }
    }
}