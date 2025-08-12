using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace miniApp.API.Services
{
    public record EmailAttachment(string FileName, string ContentType, byte[] Data);

    public class EmailService
    {
        private readonly IConfiguration _cfg;
        public EmailService(IConfiguration cfg) { _cfg = cfg; }

        public async Task SendAsync(string to, string subject, string htmlBody, IEnumerable<EmailAttachment>? attachments = null)
        {
            var host = _cfg["Smtp:Host"] ?? "";
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            var user = _cfg["Smtp:User"] ?? "";
            var pass = _cfg["Smtp:Pass"] ?? "";
            var from = _cfg["Smtp:From"] ?? user;
            var fromName = _cfg["Smtp:FromName"] ?? "miniApp";
            var enableSsl = bool.TryParse(_cfg["Smtp:Ssl"], out var ssl) ? ssl : true;

            using var msg = new MailMessage() { From = new MailAddress(from, fromName), Subject = subject, Body = htmlBody, IsBodyHtml = true };
            msg.To.Add(to);
            if (attachments != null)
            {
                foreach (var a in attachments)
                {
                    msg.Attachments.Add(new Attachment(new MemoryStream(a.Data), a.FileName, a.ContentType));
                }
            }

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(user, pass)
            };
            await smtp.SendMailAsync(msg);
        }
    }
}