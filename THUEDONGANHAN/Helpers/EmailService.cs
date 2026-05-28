using System.Net;
using System.Net.Mail;

namespace THUEDONGANHAN.Helpers
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = _configuration["SmtpSettings:Host"];
            var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "Thuê Đồ Ngắn Hạn UTE";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username!, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
