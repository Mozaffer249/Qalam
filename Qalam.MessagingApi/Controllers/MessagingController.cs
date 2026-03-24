using Microsoft.AspNetCore.Mvc;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Models.Requests;
using Qalam.MessagingApi.Models.Responses;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Controllers;

[ApiController]
[Route("api/messaging")]
public class MessagingController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly IMessageTrackingService _trackingService;
    private readonly ILogger<MessagingController> _logger;

    public MessagingController(
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        IMessageTrackingService trackingService,
        ILogger<MessagingController> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _pushService = pushService;
        _trackingService = trackingService;
        _logger = logger;
    }

    #region Email

    [HttpPost("email")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            await _trackingService.LogMessageAsync(messageId, MessageType.Email,
                request.To, request.Subject, request.Body);

            await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, request.Strategy);

            await _trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);

            return Ok(new MessageResponse
            {
                MessageId = messageId,
                Type = MessageType.Email,
                Status = MessageStatus.Sent,
                QueuedAt = DateTime.UtcNow,
                Message = $"Email sent to {request.To}",
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {To}", request.To);
            return StatusCode(500, new MessageResponse
            {
                Type = MessageType.Email,
                Status = MessageStatus.Failed,
                Message = ex.Message,
                Success = false
            });
        }
    }

    [HttpPost("email/bulk")]
    public async Task<IActionResult> SendBulkEmail([FromBody] List<SendEmailRequest> requests)
    {
        var results = new List<MessageResponse>();

        foreach (var request in requests)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                await _trackingService.LogMessageAsync(messageId, MessageType.Email,
                    request.To, request.Subject, request.Body);

                await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, request.Strategy);
                await _trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);

                results.Add(new MessageResponse
                {
                    MessageId = messageId,
                    Type = MessageType.Email,
                    Status = MessageStatus.Sent,
                    QueuedAt = DateTime.UtcNow,
                    Message = $"Email sent to {request.To}",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk email to: {To}", request.To);
                results.Add(new MessageResponse
                {
                    Type = MessageType.Email,
                    Status = MessageStatus.Failed,
                    Message = ex.Message,
                    Success = false
                });
            }
        }

        return Ok(results);
    }

    #endregion

    #region SMS

    [HttpPost("sms")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            await _trackingService.LogMessageAsync(messageId, MessageType.SMS,
                request.PhoneNumber, "SMS", request.Content);

            await _smsService.SendSmsAsync(request.PhoneNumber, request.Content, request.Strategy);
            await _trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);

            return Ok(new MessageResponse
            {
                MessageId = messageId,
                Type = MessageType.SMS,
                Status = MessageStatus.Sent,
                QueuedAt = DateTime.UtcNow,
                Message = $"SMS sent to {request.PhoneNumber}",
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new MessageResponse
            {
                Type = MessageType.SMS,
                Status = MessageStatus.Failed,
                Message = ex.Message,
                Success = false
            });
        }
    }

    [HttpPost("sms/bulk")]
    public async Task<IActionResult> SendBulkSms([FromBody] List<SendSmsRequest> requests)
    {
        var results = new List<MessageResponse>();

        foreach (var request in requests)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                await _trackingService.LogMessageAsync(messageId, MessageType.SMS,
                    request.PhoneNumber, "SMS", request.Content);

                await _smsService.SendSmsAsync(request.PhoneNumber, request.Content, request.Strategy);
                await _trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);

                results.Add(new MessageResponse
                {
                    MessageId = messageId,
                    Type = MessageType.SMS,
                    Status = MessageStatus.Sent,
                    QueuedAt = DateTime.UtcNow,
                    Message = $"SMS sent to {request.PhoneNumber}",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new MessageResponse
                {
                    Type = MessageType.SMS,
                    Status = MessageStatus.Failed,
                    Message = ex.Message,
                    Success = false
                });
            }
        }

        return Ok(results);
    }

    #endregion

    #region Push Notifications

    [HttpPost("push")]
    public async Task<IActionResult> SendPushNotification([FromBody] SendPushNotificationRequest request)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();
            await _trackingService.LogMessageAsync(messageId, MessageType.PushNotification,
                request.DeviceToken, request.Title, request.Body);

            await _pushService.SendPushNotificationAsync(
                request.DeviceToken, request.Title, request.Body, request.Strategy, request.Data);

            await _trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);

            return Ok(new MessageResponse
            {
                MessageId = messageId,
                Type = MessageType.PushNotification,
                Status = MessageStatus.Sent,
                QueuedAt = DateTime.UtcNow,
                Message = $"Push notification sent to {request.DeviceToken}",
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push to: {DeviceToken}", request.DeviceToken);
            return StatusCode(500, new MessageResponse
            {
                Type = MessageType.PushNotification,
                Status = MessageStatus.Failed,
                Message = ex.Message,
                Success = false
            });
        }
    }

    [HttpPost("push/bulk")]
    public async Task<IActionResult> SendBulkPushNotification([FromBody] List<SendPushNotificationRequest> requests)
    {
        var results = new List<MessageResponse>();

        foreach (var request in requests)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                await _trackingService.LogMessageAsync(messageId, MessageType.PushNotification,
                    request.DeviceToken, request.Title, request.Body);

                await _pushService.SendPushNotificationAsync(
                    request.DeviceToken, request.Title, request.Body, request.Strategy, request.Data);

                await _trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);

                results.Add(new MessageResponse
                {
                    MessageId = messageId,
                    Type = MessageType.PushNotification,
                    Status = MessageStatus.Sent,
                    QueuedAt = DateTime.UtcNow,
                    Message = $"Push sent to {request.DeviceToken}",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new MessageResponse
                {
                    Type = MessageType.PushNotification,
                    Status = MessageStatus.Failed,
                    Message = ex.Message,
                    Success = false
                });
            }
        }

        return Ok(results);
    }

    #endregion

    #region Tracking

    [HttpGet("status/{messageId}")]
    public async Task<IActionResult> GetStatus(string messageId)
    {
        var log = await _trackingService.GetMessageStatusAsync(messageId);
        if (log == null)
            return NotFound(new { Message = $"Message '{messageId}' not found." });

        return Ok(new MessageStatusResponse
        {
            MessageId = log.MessageId,
            Type = log.Type,
            Status = log.Status,
            QueuedAt = log.QueuedAt,
            ProcessedAt = log.ProcessedAt,
            DeliveredAt = log.DeliveredAt,
            ErrorMessage = log.ErrorMessage,
            RetryCount = log.RetryCount
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var history = await _trackingService.GetMessageHistoryAsync(page, pageSize);
        return Ok(history);
    }

    #endregion

    #region Health

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Qalam.MessagingApi",
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion
}
