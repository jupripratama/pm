using MailKit.Net.Smtp;
using MimeKit;

namespace Pm.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("PM KPC System", _config["Email:From"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"]!),
                MailKit.Security.SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(_config["Email:SmtpUser"], _config["Email:SmtpPass"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendTemuanCreatedEmailAsync(int temuanId, string ruang, string temuan, string picEmail)
        {
            var subject = $"[KPC] Temuan Baru #{temuanId} - {ruang}";
            var body = $@"
                <h3>Temuan Baru Telah Dibuat</h3>
                <p><strong>ID:</strong> {temuanId}</p>
                <p><strong>Ruang:</strong> {ruang}</p>
                <p><strong>Temuan:</strong> {temuan}</p>
                <hr>
                <p><em>Email ini otomatis dari sistem PM KPC</em></p>
            ";

            await SendEmailAsync(picEmail, subject, body);
        }

        public async Task SendStatusClosedEmailAsync(int temuanId, string ruang, string picEmail)
        {
            var subject = $"[KPC] Temuan #{temuanId} Telah Ditutup";
            var body = $@"
                <h3>Temuan Telah Ditutup</h3>
                <p><strong>ID:</strong> {temuanId}</p>
                <p><strong>Ruang:</strong> {ruang}</p>
                <p>Status: <span style='color:green;font-weight:bold'>CLOSED</span></p>
                <hr>
                <p>Terima kasih atas tindak lanjutnya!</p>
            ";

            await SendEmailAsync(picEmail, subject, body);
        }
    }
}