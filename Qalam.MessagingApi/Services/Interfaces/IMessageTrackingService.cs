using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Services.Interfaces;

public interface IMessageTrackingService
{
    Task<MessageLog> LogMessageAsync(string messageId, MessageType type, string recipient,
        string subject, string content, MessageStatus status = MessageStatus.Queued);
    Task UpdateStatusAsync(string messageId, MessageStatus status, string? errorMessage = null);
    Task<MessageLog?> GetMessageStatusAsync(string messageId);
    Task<List<MessageLog>> GetMessageHistoryAsync(int pageNumber = 1, int pageSize = 50);
}
