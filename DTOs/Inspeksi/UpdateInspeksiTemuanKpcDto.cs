using Microsoft.AspNetCore.Mvc;

public class UpdateInspeksiTemuanKpcDto
{
    public string? NoFollowUp { get; set; }
    public string? PerbaikanDilakukan { get; set; }
    public DateTime? TanggalPerbaikan { get; set; }
    public DateTime? TanggalSelesaiPerbaikan { get; set; }
    public string? PicPelaksana { get; set; }
    public string? Status { get; set; }
    public string? Keterangan { get; set; }

    [FromForm(Name = "fotoHasilFiles")]
    public List<IFormFile>? FotoHasilFiles { get; set; }
}