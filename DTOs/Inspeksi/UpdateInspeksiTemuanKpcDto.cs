using Microsoft.AspNetCore.Mvc;

public class UpdateInspeksiTemuanKpcDto
{
    // ✅ TAMBAHKAN SEMUA FIELD YANG BISA DIUPDATE
     public string? Ruang { get; set; }
    public string? Temuan { get; set; }
    public string? KategoriTemuan { get; set; }
    public string? Inspector { get; set; }
    public string? Severity { get; set; }
    public DateTime? TanggalTemuan { get; set; }
    public string? NoFollowUp { get; set; }
    public string? PerbaikanDilakukan { get; set; }
    public DateTime? TanggalPerbaikan { get; set; }
    public DateTime? TanggalSelesaiPerbaikan { get; set; }
    public string? PicPelaksana { get; set; }
    public string? Status { get; set; }
    public string? Keterangan { get; set; }

    [FromForm(Name = "fotoTemuanFiles")]
    public List<IFormFile>? FotoTemuanFiles { get; set; }

    [FromForm(Name = "fotoHasilFiles")]
    public List<IFormFile>? FotoHasilFiles { get; set; }
}