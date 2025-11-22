using Microsoft.AspNetCore.Mvc;

public class UpdateInspeksiTemuanKpcDto
{
    // ✅ FIELD YANG BISA DIUPDATE
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

    // ✅ FLAGS UNTUK MENGOSONGKAN FIELD
    public bool ClearNoFollowUp { get; set; }
    public bool ClearPicPelaksana { get; set; }
    public bool ClearPerbaikanDilakukan { get; set; }
    public bool ClearKeterangan { get; set; }

    [FromForm(Name = "fotoTemuanFiles")]
    public List<IFormFile>? FotoTemuanFiles { get; set; }

    [FromForm(Name = "fotoHasilFiles")]
    public List<IFormFile>? FotoHasilFiles { get; set; }
}