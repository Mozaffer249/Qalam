using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IMessageLogRepository : IGenericRepositoryAsync<MessageLog>
{
    Task<MessageLog?> GetByMessageIdAsync(string messageId);
    Task<List<MessageLog>> GetHistoryAsync(int pageNumber, int pageSize);
}
