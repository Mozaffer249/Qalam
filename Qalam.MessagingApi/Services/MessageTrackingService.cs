using Microsoft.EntityFrameworkCore;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class MessageTrackingService : IMessageTrackingService
{
    private readonly MessagingDbContext _context;
    private readonly ILogger<MessageTrackingService> _logger;

    public MessageTrackingService(MessagingDbContext context, ILogger<MessageTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MessageLog> LogMessageAsync(string messageId, MessageType type, string recipient,
        string subject, string content, MessageStatus status = MessageStatus.Queued)
    {
        try
        {
            var messageLog = new MessageLog
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                Type = type,
                Recipient = recipient,
                Subject = subject,
                Content = content,
                Status = status,
                QueuedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            _context.MessageLogs.Add(messageLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message logged: {MessageId}, Type: {Type}, Recipient: {Recipient}",
                messageId, type, recipient);

            return messageLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log message: {MessageId}", messageId);
            throw;
        }
    }

    public async Task UpdateStatusAsync(string messageId, MessageStatus status, string? errorMessage = null)
    {
        try
        {
            var messageLog = await _context.MessageLogs
                .FirstOrDefaultAsync(m => m.MessageId == messageId);

            if (messageLog != null)
            {
                messageLog.Status = status;
                messageLog.ErrorMessage = errorMessage;

                if (status == MessageStatus.Processing)
                    messageLog.ProcessedAt = DateTime.UtcNow;
                else if (status == MessageStatus.Sent || status == MessageStatus.Delivered)
                    messageLog.DeliveredAt = DateTime.UtcNow;

                if (status == MessageStatus.Failed)
                    messageLog.RetryCount++;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Message status updated: {MessageId}, Status: {Status}", messageId, status);
            }
            else
            {
                _logger.LogWarning("Message not found for status update: {MessageId}", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update message status: {MessageId}", messageId);
            throw;
        }
    }

    public async Task<MessageLog?> GetMessageStatusAsync(string messageId)
    {
        return await _context.MessageLogs.FirstOrDefaultAsync(m => m.MessageId == messageId);
    }

    public async Task<List<MessageLog>> GetMessageHistoryAsync(int pageNumber = 1, int pageSize = 50)
    {
        return await _context.MessageLogs
            .OrderByDescending(m => m.QueuedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
