using CRM.Server.Services.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace CRM.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage(); // ✅ Framework class (MailKit)

            message.From.Add(new MailboxAddress(
                _config["SMTP:FromName"],   // ✅ Your config
                _config["SMTP:From"]        // ✅ Your config
            ));

            message.To.Add(MailboxAddress.Parse(toEmail)); // ✅ Framework
            message.Subject = subject;                    // ✅ Your input

            message.Body = new TextPart("plain") { Text = body }; // ✅ Framework

            using var client = new SmtpClient(); // ✅ Framework

            await client.ConnectAsync(
                _config["SMTP:Host"],                          // ✅ Your config
                int.Parse(_config["SMTP:Port"]!),             // ✅ Your config
                MailKit.Security.SecureSocketOptions.StartTls // ✅ Correct for 587
            );

            await client.AuthenticateAsync(
                _config["SMTP:User"],     // ✅ Your config
                _config["SMTP:Password"] // ✅ Your config
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

    }
}
