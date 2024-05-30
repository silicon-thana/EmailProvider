using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailProvider.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmailProvider.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailClient _emailClient;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailClient emailClient, ILogger<EmailService> logger)
        {
            _emailClient = emailClient;
            _logger = logger;
        }

        public EmailRequest UnpackEmailRequest(ServiceBusReceivedMessage message)
        {
            try
            {
                var emailRequest = JsonConvert.DeserializeObject<EmailRequest>(message.Body.ToString());
                if (emailRequest != null)
                {
                    _logger.LogInformation("Successfully unpacked email request");
                    return emailRequest;
                }
                else
                {
                    _logger.LogError("Email request is null after deserialization");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : EmailService.UnpackEmailRequest :: {ex.Message}");
            }

            return null!;
        }

        public bool SendEmail(EmailRequest emailRequest)
        {
            try
            {
                var result = _emailClient.Send(
                    WaitUntil.Completed,
                    senderAddress: Environment.GetEnvironmentVariable("SenderAddress"),
                    recipientAddress: emailRequest.RecipientAddress,
                    subject: emailRequest.Subject,
                    htmlContent: emailRequest.HtmlContent,
                    plainTextContent: emailRequest.PlainTextContent);

                if (result.HasCompleted)
                {
                    _logger.LogInformation("Email sent successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("Email sending did not complete successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : EmailService.SendEmail :: {ex.Message}");
            }
            return false;
        }
    }
}
