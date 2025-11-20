namespace Pm.Services
{
    public interface IEmailService
    {
        Task SendTemuanCreatedEmailAsync(int temuanId, string ruang, string temuan, string picEmail);
        Task SendStatusClosedEmailAsync(int temuanId, string ruang, string picEmail);
    }
}