using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Services
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(string email, string displayName, string link);
        Task SendPasswordResetAsync(string email, string displayName, string link);
        Task SendClaimReceivedAsync(string posterEmail, string itemTitle, string claimantName, string claimMessage);
        Task SendClaimStatusAsync(string claimantEmail, string itemTitle, ClaimStatus newStatus);
        Task SendSearchAlertAsync(string email, string query, string itemTitle, string link);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["Email:From"] ?? "noreply@campus.edu"));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var smtp = new SmtpClient();
                // To use Gmail, enable App Passwords
                await smtp.ConnectAsync(_config["Email:Host"] ?? "smtp.gmail.com", int.Parse(_config["Email:Port"] ?? "587"), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            }
        }

        public async Task SendConfirmationAsync(string email, string displayName, string link)
        {
            var body = $"<h3>Welcome to Campus Lost & Found, {displayName}!</h3><p>Please confirm your account by <a href='{link}'>clicking here</a>.</p>";
            await SendEmailAsync(email, "Confirm your account", body);
        }

        public async Task SendPasswordResetAsync(string email, string displayName, string link)
        {
            var body = $"<h3>Password Reset Request</h3><p>Hi {displayName}, please reset your password by <a href='{link}'>clicking here</a>.</p>";
            await SendEmailAsync(email, "Reset Password", body);
        }

        public async Task SendClaimReceivedAsync(string posterEmail, string itemTitle, string claimantName, string claimMessage)
        {
            var body = $"<h3>New Claim for '{itemTitle}'</h3><p>{claimantName} has submitted a claim request with the following message:</p><blockquote>{claimMessage}</blockquote><p>Please log in to review the claim.</p>";
            await SendEmailAsync(posterEmail, $"New Claim on your item: {itemTitle}", body);
        }

        public async Task SendClaimStatusAsync(string claimantEmail, string itemTitle, ClaimStatus newStatus)
        {
            var body = $"<h3>Claim Status Update</h3><p>Your claim for '{itemTitle}' has been marked as <strong>{newStatus}</strong> by the owner.</p>";
            await SendEmailAsync(claimantEmail, $"Claim {newStatus}: {itemTitle}", body);
        }

        public async Task SendSearchAlertAsync(string email, string query, string itemTitle, string link)
        {
            var body = $"<h3>Saved Search Alert: {query}</h3><p>A new item matching your search has been posted: <strong>{itemTitle}</strong></p><p><a href='{link}'>View Item</a></p>";
            await SendEmailAsync(email, $"New Item Alert: {query}", body);
        }
    }
}
