namespace ApplicationSecurity_Backend.Services
{
    using MailKit.Net.Smtp;
    using MimeKit;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Options;
    using System.Threading.Tasks;

    public class EmailSender : IEmailSender
    {
        private readonly EmailSenderOptions _emailSenderOptions;

        public EmailSender(IOptions<EmailSenderOptions> emailSenderOptions)
        {
            _emailSenderOptions = emailSenderOptions.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSenderOptions.EmailFromName, _emailSenderOptions.EmailFrom));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = htmlMessage
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailSenderOptions.SmtpServer, _emailSenderOptions.SmtpPort, true);
                await client.AuthenticateAsync(_emailSenderOptions.SmtpUsername, _emailSenderOptions.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }

    public class EmailSenderOptions
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string EmailFrom { get; set; }

        public string EmailFromName { get; set; }

    }
}
