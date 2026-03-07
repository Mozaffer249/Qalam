using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class MessageLogRepository : GenericRepositoryAsync<MessageLog>, IMessageLogRepository
{
    private readonly DbSet<MessageLog> _messageLogs;

    public MessageLogRepository(ApplicationDBContext context) : base(context)
    {
        _messageLogs = context.Set<MessageLog>();
    }

    public async Task<MessageLog?> GetByMessageIdAsync(string messageId)
    {
        return await _messageLogs.FirstOrDefaultAsync(m => m.MessageId == messageId);
    }

    public async Task<List<MessageLog>> GetHistoryAsync(int pageNumber, int pageSize)
    {
        return await _messageLogs
            .OrderByDescending(m => m.QueuedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
