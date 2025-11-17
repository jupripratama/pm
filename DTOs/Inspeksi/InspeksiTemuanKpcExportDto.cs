namespace Pm.DTOs.Export
{
    public class InspeksiTemuanKpcExportDto
    {
        public string No { get; set; } = "";
        public string Ruang { get; set; } = "";
        public string Temuan { get; set; } = "";
        public string Severity { get; set; } = "";
        public string TanggalTemuan { get; set; } = "";
        public string NoFollowUp { get; set; } = "-";
        public string FollowUpRef { get; set; } = "-";
        public string PerbaikanDilakukan { get; set; } = "-";
        public string TanggalPerbaikan { get; set; } = "-";
        public string PicPelaksana { get; set; } = "-";
        public string Status { get; set; } = "";
        public string Keterangan { get; set; } = "-";
        public string FotoTemuan { get; set; } = "-";
        public string FotoHasil { get; set; } = "-";
        public string DibuatOleh { get; set; } = "";
        public string DibuatPada { get; set; } = "";
    }
}