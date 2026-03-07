using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Qalam.Service.Implementations
{
    public class SmsService : ISmsService
    {
        private readonly SmsSettings _smsSettings;
        private readonly ILogger<SmsService> _logger;
        private readonly IRabbitMQService _rabbitMQService;

        public SmsService(
            IOptions<SmsSettings> smsSettings,
            ILogger<SmsService> logger,
            IRabbitMQService rabbitMQService)
        {
            _smsSettings = smsSettings.Value;
            _logger = logger;
            _rabbitMQService = rabbitMQService;

            if (!string.IsNullOrEmpty(_smsSettings.AccountSid) && !string.IsNullOrEmpty(_smsSettings.AuthToken))
            {
                TwilioClient.Init(_smsSettings.AccountSid, _smsSettings.AuthToken);
            }
        }

        public async Task SendSmsAsync(string phoneNumber, string content)
        {
            await SendSmsAsync(phoneNumber, content, SendingStrategy.Fallback);
        }

        public async Task SendSmsAsync(string phoneNumber, string content, SendingStrategy strategy)
        {
            switch (strategy)
            {
                case SendingStrategy.Direct:
                    await SendDirectAsync(phoneNumber, content);
                    break;
                case SendingStrategy.Queued:
                    await QueueSmsAsync(phoneNumber, content);
                    break;
                case SendingStrategy.Fallback:
                    await SendWithFallbackAsync(phoneNumber, content);
                    break;
                default:
                    throw new ArgumentException($"Unknown SMS sending strategy: {strategy}");
            }
        }

        private async Task SendDirectAsync(string phoneNumber, string content)
        {
            try
            {
                var message = await MessageResource.CreateAsync(
                    body: content,
                    from: new PhoneNumber(_smsSettings.FromNumber),
                    to: new PhoneNumber(phoneNumber));

                _logger.LogInformation("SMS sent successfully (Direct) to: {PhoneNumber}, SID: {Sid}",
                    phoneNumber, message.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to: {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        private async Task QueueSmsAsync(string phoneNumber, string content)
        {
            await _rabbitMQService.QueueSmsAsync(new SmsMessage
            {
                PhoneNumber = phoneNumber,
                Content = content
            });
            _logger.LogInformation("SMS queued for delivery to: {PhoneNumber}", phoneNumber);
        }

        private async Task SendWithFallbackAsync(string phoneNumber, string content)
        {
            try
            {
                await SendDirectAsync(phoneNumber, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to: {PhoneNumber}. Error: {Message}", phoneNumber, ex.Message);
                try
                {
                    await QueueSmsAsync(phoneNumber, content);
                    _logger.LogInformation("SMS queued for later delivery (Fallback) to: {PhoneNumber}", phoneNumber);
                }
                catch (Exception queueEx)
                {
                    _logger.LogError(queueEx, "Failed to queue SMS to: {PhoneNumber}. SMS will be lost.", phoneNumber);
                    throw;
                }
            }
        }
    }
}
