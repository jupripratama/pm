// Models/EmailTemplate.cs (BONUS KALAU MAU CUSTOM EMAIL)
using Pm.Models;

public class EmailTemplate
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // TemuanCreated, StatusClosed, dll
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}