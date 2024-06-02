using EmailProvider.Models;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmailProvider.Functions
{
    public class EmailHttpTrigger
    {
        private readonly ILogger<EmailHttpTrigger> _logger;
        private readonly IEmailService _emailService;

        public EmailHttpTrigger(ILogger<EmailHttpTrigger> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [Function("SendEmailHttpTrigger")]
        public async Task<HttpResponseData> SendEmailHttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var emailRequest = JsonConvert.DeserializeObject<EmailRequest>(requestBody);

                if (emailRequest != null && !string.IsNullOrEmpty(emailRequest.RecipientAddress))
                {
                    _logger.LogInformation($"Sending email to {emailRequest.RecipientAddress}");

                    if (_emailService.SendEmail(emailRequest))
                    {
                        response.StatusCode = System.Net.HttpStatusCode.OK;
                        await response.WriteStringAsync($"Email sent to {emailRequest.RecipientAddress}");
                        _logger.LogInformation($"Email sent to {emailRequest.RecipientAddress}");
                    }
                    else
                    {
                        response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                        await response.WriteStringAsync($"Failed to send email to {emailRequest.RecipientAddress}");
                        _logger.LogError($"Failed to send email to {emailRequest.RecipientAddress}");
                    }
                }
                else
                {
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Invalid email request or recipient address is empty");
                    _logger.LogError("Invalid email request or recipient address is empty");
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"ERROR: {ex.Message}");
                _logger.LogError($"ERROR : EmailHttpTrigger.SendEmailHttpTrigger :: {ex.Message}");
            }

            return response;
        }
    }
}
