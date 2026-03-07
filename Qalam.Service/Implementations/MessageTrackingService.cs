using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations
{
    public class MessageTrackingService : IMessageTrackingService
    {
        private readonly IMessageLogRepository _messageLogRepository;
        private readonly ILogger<MessageTrackingService> _logger;

        public MessageTrackingService(IMessageLogRepository messageLogRepository, ILogger<MessageTrackingService> logger)
        {
            _messageLogRepository = messageLogRepository;
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

                await _messageLogRepository.AddAsync(messageLog);
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
                var messageLog = await _messageLogRepository.GetByMessageIdAsync(messageId);

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

                    await _messageLogRepository.UpdateAsync(messageLog);
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
            try
            {
                return await _messageLogRepository.GetByMessageIdAsync(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get message status: {MessageId}", messageId);
                throw;
            }
        }

        public async Task<List<MessageLog>> GetMessageHistoryAsync(int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                return await _messageLogRepository.GetHistoryAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get message history");
                throw;
            }
        }
    }
}
