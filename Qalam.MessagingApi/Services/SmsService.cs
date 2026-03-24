using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Qalam.MessagingApi.Services;

public class SmsService : ISmsService
{
    private readonly SmsSettings _smsSettings;
    private readonly ILogger<SmsService> _logger;
    private readonly IMessageQueueService _messageQueueService;

    public SmsService(
        IOptions<SmsSettings> smsSettings,
        ILogger<SmsService> logger,
        IMessageQueueService messageQueueService)
    {
        _smsSettings = smsSettings.Value;
        _logger = logger;
        _messageQueueService = messageQueueService;

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
        await _messageQueueService.QueueSmsAsync(new SmsMessage
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
            _logger.LogWarning(ex, "Direct SMS failed to: {PhoneNumber}, falling back to queue", phoneNumber);
            try
            {
                await QueueSmsAsync(phoneNumber, content);
            }
            catch (Exception queueEx)
            {
                _logger.LogError(queueEx, "Failed to queue SMS to: {PhoneNumber}. SMS will be lost.", phoneNumber);
                throw;
            }
        }
    }
}
