using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dierenwinkel.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Dierenwinkel.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string to, string customerName, string orderNumber, decimal totalAmount, List<string> orderItems)
        {
            try
            {
                var subject = $"Bevestiging van uw bestelling #{orderNumber}";
                var body = GenerateOrderConfirmationEmailBody(customerName, orderNumber, totalAmount, orderItems);

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email to {Email}", to);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("SMTP settings not configured. Email not sent.");
                    return false;
                }

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? username, fromName ?? "Pet Shop"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", to);
                return false;
            }
        }

        private string GenerateOrderConfirmationEmailBody(string customerName, string orderNumber, decimal totalAmount, List<string> orderItems)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='utf-8'>");
            sb.AppendLine("    <title>Bevestiging bestelling</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }");
            sb.AppendLine("        .container { max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .header { text-align: center; margin-bottom: 30px; }");
            sb.AppendLine("        .header h1 { color: #2c3e50; margin: 0; }");
            sb.AppendLine("        .order-info { background-color: #ecf0f1; padding: 20px; border-radius: 5px; margin-bottom: 20px; }");
            sb.AppendLine("        .order-items { margin-bottom: 20px; }");
            sb.AppendLine("        .order-items h3 { color: #34495e; margin-top: 0; }");
            sb.AppendLine("        .order-items ul { list-style-type: none; padding: 0; }");
            sb.AppendLine("        .order-items li { padding: 10px; border-bottom: 1px solid #bdc3c7; }");
            sb.AppendLine("        .total { text-align: right; font-size: 18px; font-weight: bold; color: #27ae60; }");
            sb.AppendLine("        .footer { text-align: center; margin-top: 30px; color: #7f8c8d; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class='container'>");
            sb.AppendLine("        <div class='header'>");
            sb.AppendLine("            <h1>🐾 Pet Shop</h1>");
            sb.AppendLine("            <h2>Bedankt voor uw bestelling!</h2>");
            sb.AppendLine("        </div>");
            sb.AppendLine($"        <p>Beste {customerName},</p>");
            sb.AppendLine("        <p>We hebben uw bestelling succesvol ontvangen en bevestigen deze hierbij.</p>");
            sb.AppendLine("        <div class='order-info'>");
            sb.AppendLine($"            <h3>Bestelling #{orderNumber}</h3>");
            sb.AppendLine($"            <p><strong>Datum:</strong> {DateTime.Now:dd MMMM yyyy}</p>");
            sb.AppendLine($"            <p><strong>Status:</strong> In behandeling</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='order-items'>");
            sb.AppendLine("            <h3>Bestelde artikelen:</h3>");
            sb.AppendLine("            <ul>");
            
            foreach (var item in orderItems)
            {
                sb.AppendLine($"                <li>{item}</li>");
            }
            
            sb.AppendLine("            </ul>");
            sb.AppendLine("        </div>");
            sb.AppendLine($"        <div class='total'>Totaalbedrag: €{totalAmount:F2}</div>");
            sb.AppendLine("        <p>Uw bestelling wordt zo spoedig mogelijk verwerkt en verzonden. U ontvangt een verzendbevestiging zodra uw bestelling onderweg is.</p>");
            sb.AppendLine("        <p>Voor vragen over uw bestelling kunt u contact met ons opnemen via info@petshop.nl of telefonisch via 0800-PETSHOP.</p>");
            sb.AppendLine("        <div class='footer'>");
            sb.AppendLine("            <p>Met vriendelijke groet,<br>Het Pet Shop Team</p>");
            sb.AppendLine("            <p><small>Dit is een automatisch gegenereerd bericht. Antwoord niet op deze e-mail.</small></p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
