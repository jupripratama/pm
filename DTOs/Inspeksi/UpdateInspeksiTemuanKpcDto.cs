// Pm/DTOs/UpdateInspeksiTemuanKpcDto.cs - REPLACE SELURUH FILE
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Pm.DTOs
{
    public class UpdateInspeksiTemuanKpcDto
    {
        // ✅ BASIC FIELDS - REQUIRED FIELDS (tidak bisa null/kosong)
        public string? Ruang { get; set; }
        public string? Temuan { get; set; }
        public string? Severity { get; set; }
        public DateTime? TanggalTemuan { get; set; }
        public string? Status { get; set; }

        // ✅ OPTIONAL TEXT FIELDS - Bisa dikosongkan
        public string? KategoriTemuan { get; set; }
        public string? Inspector { get; set; }
        public string? NoFollowUp { get; set; }
        public string? PicPelaksana { get; set; }
        public string? PerbaikanDilakukan { get; set; }
        public string? Keterangan { get; set; }

        // ✅ DATE FIELDS - Bisa dikosongkan
        public DateTime? TanggalPerbaikan { get; set; }
        public DateTime? TanggalSelesaiPerbaikan { get; set; }

        // ✅ CLEAR FLAGS - Untuk membedakan "tidak diubah" vs "dikosongkan"
        public bool ClearKategoriTemuan { get; set; }
        public bool ClearInspector { get; set; }
        public bool ClearNoFollowUp { get; set; }
        public bool ClearPicPelaksana { get; set; }
        public bool ClearPerbaikanDilakukan { get; set; }
        public bool ClearKeterangan { get; set; }
        public bool ClearTanggalPerbaikan { get; set; }
        public bool ClearTanggalSelesaiPerbaikan { get; set; }

        // ✅ FILE UPLOADS
        [FromForm(Name = "fotoTemuanFiles")]
        public List<IFormFile>? FotoTemuanFiles { get; set; }

        [FromForm(Name = "fotoHasilFiles")]
        public List<IFormFile>? FotoHasilFiles { get; set; }
    }
}