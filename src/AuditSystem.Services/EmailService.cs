using AuditSystem.Domain.Entities;
using AuditSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AuditSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailNotificationMessage message)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var mailMessage = CreateMailMessage(message);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {RecipientEmail} with subject {Subject}", 
                    message.RecipientEmail, message.Subject);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {RecipientEmail} with subject {Subject}", 
                    message.RecipientEmail, message.Subject);
                return false;
            }
        }

        public async Task<bool> SendBulkEmailAsync(IEnumerable<EmailNotificationMessage> messages)
        {
            var successCount = 0;
            var totalCount = 0;

            foreach (var message in messages)
            {
                totalCount++;
                if (await SendEmailAsync(message))
                {
                    successCount++;
                }
            }

            _logger.LogInformation("Bulk email sending completed: {SuccessCount}/{TotalCount} emails sent successfully", 
                successCount, totalCount);

            return successCount == totalCount;
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient
            {
                Host = _settings.SmtpHost,
                Port = _settings.SmtpPort,
                EnableSsl = _settings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = _settings.TimeoutSeconds * 1000
            };
        }

        private MailMessage CreateMailMessage(EmailNotificationMessage message)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(message.FromEmail ?? _settings.FromEmail, message.FromName ?? _settings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml,
                Priority = GetMailPriority(message.Priority)
            };

            // Add recipients
            mailMessage.To.Add(message.RecipientEmail);

            // Add CC recipients
            if (message.Cc != null)
            {
                foreach (var cc in message.Cc)
                {
                    mailMessage.CC.Add(cc);
                }
            }

            // Add BCC recipients
            if (message.Bcc != null)
            {
                foreach (var bcc in message.Bcc)
                {
                    mailMessage.Bcc.Add(bcc);
                }
            }

            // Add reply-to
            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                mailMessage.ReplyToList.Add(message.ReplyTo);
            }

            // Add attachments
            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var mailAttachment = new Attachment(
                        new System.IO.MemoryStream(attachment.Content), 
                        attachment.FileName, 
                        attachment.ContentType);

                    if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
                    {
                        mailAttachment.ContentId = attachment.ContentId;
                        mailAttachment.ContentDisposition.Inline = true;
                    }

                    mailMessage.Attachments.Add(mailAttachment);
                }
            }

            return mailMessage;
        }

        private MailPriority GetMailPriority(string priority)
        {
            return priority?.ToLower() switch
            {
                "urgent" => MailPriority.High,
                "high" => MailPriority.High,
                "medium" => MailPriority.Normal,
                "low" => MailPriority.Low,
                _ => MailPriority.Normal
            };
        }
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "localhost";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "noreply@auditsystem.com";
        public string FromName { get; set; } = "Audit System";
        public int TimeoutSeconds { get; set; } = 30;
    }
} 