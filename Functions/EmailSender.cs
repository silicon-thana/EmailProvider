using System;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailProvider.Models;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmailProvider.Functions
{
    public class EmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IEmailService _emailService;

        public EmailSender(ILogger<EmailSender> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [Function(nameof(EmailSender))]
        public async Task Run(
            [ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            try
            {
                var emailRequest = _emailService.UnpackEmailRequest(message);
                if (emailRequest != null && !string.IsNullOrEmpty(emailRequest.RecipientAddress))
                {
                    _logger.LogInformation($"Sending email to {emailRequest.RecipientAddress}");

                    if (_emailService.SendEmail(emailRequest))
                    {
                        await messageActions.CompleteMessageAsync(message);
                        _logger.LogInformation($"Email sent to {emailRequest.RecipientAddress} and message completed");
                    }
                    else
                    {
                        _logger.LogError($"Failed to send email to {emailRequest.RecipientAddress}");
                        // Optionally, you can handle dead-lettering or retry logic here
                    }
                }
                else
                {
                    _logger.LogError("Invalid email request or recipient address is empty");
                    // Optionally, handle invalid message
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : EmailSender.Run :: {ex.Message}");
                // Optionally, handle error (e.g., dead-letter message)
            }
        }
    }
}
