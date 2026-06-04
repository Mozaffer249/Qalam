using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class OfferConversationRepository : GenericRepositoryAsync<OfferConversation>, IOfferConversationRepository
{
    private readonly ApplicationDBContext _context;

    public OfferConversationRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<OfferConversation?> GetByRequestAndTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.OfferConversations
            .FirstOrDefaultAsync(c => c.SessionRequestId == requestId && c.TeacherId == teacherId, cancellationToken);
    }

    public async Task<OfferConversation> EnsureExistsAsync(int requestId, int teacherId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByRequestAndTeacherAsync(requestId, teacherId, cancellationToken);
        if (existing != null) return existing;

        var now = DateTime.UtcNow;
        var conv = new OfferConversation
        {
            SessionRequestId = requestId,
            TeacherId = teacherId,
            CreatedAt = now
        };
        await _context.OfferConversations.AddAsync(conv, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return conv;
    }

    public async Task SetCurrentOfferAsync(int conversationId, int? offerId, CancellationToken cancellationToken = default)
    {
        var conv = await _context.OfferConversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conv == null) return;
        conv.SessionOfferId = offerId;
        conv.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OfferConversationDto?> GetHeaderDtoAsync(int conversationId, ConversationCaller caller, CancellationToken cancellationToken = default)
    {
        var row = await _context.OfferConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                c.Id,
                c.SessionRequestId,
                c.SessionOfferId,
                c.LastMessageAt,
                c.StudentLastReadAt,
                c.TeacherLastReadAt,
                TeacherUserId = c.Teacher.UserId,
                TeacherFirstName = c.Teacher.User != null ? c.Teacher.User.FirstName : null,
                TeacherLastName = c.Teacher.User != null ? c.Teacher.User.LastName : null,
                StudentUserId = (int?)c.OpenSessionRequest.RequestedByUserId,
                StudentFirstName = c.OpenSessionRequest.Student != null && c.OpenSessionRequest.Student.User != null
                    ? c.OpenSessionRequest.Student.User.FirstName
                    : null,
                StudentLastName = c.OpenSessionRequest.Student != null && c.OpenSessionRequest.Student.User != null
                    ? c.OpenSessionRequest.Student.User.LastName
                    : null,
                UnreadCount = c.Messages.Count(m =>
                    m.SenderUserId != null
                    && (caller == ConversationCaller.Teacher
                            ? (c.TeacherLastReadAt == null || m.SentAt > c.TeacherLastReadAt)
                            : (c.StudentLastReadAt == null || m.SentAt > c.StudentLastReadAt)))
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return null;

        var participants = new List<ConversationParticipantDto>();
        if (row.TeacherUserId.HasValue)
        {
            participants.Add(new ConversationParticipantDto
            {
                UserId = row.TeacherUserId.Value,
                DisplayName = ((row.TeacherFirstName ?? "") + " " + (row.TeacherLastName ?? "")).Trim(),
                Role = "Teacher"
            });
        }
        if (row.StudentUserId.HasValue)
        {
            participants.Add(new ConversationParticipantDto
            {
                UserId = row.StudentUserId.Value,
                DisplayName = ((row.StudentFirstName ?? "") + " " + (row.StudentLastName ?? "")).Trim(),
                Role = "Student"
            });
        }

        return new OfferConversationDto
        {
            ConversationId = row.Id,
            OfferId = row.SessionOfferId ?? 0,
            LastMessageAt = row.LastMessageAt,
            UnreadCount = row.UnreadCount,
            Participants = participants
        };
    }

    public async Task<OfferMessage> AppendMessageAsync(int conversationId, int? senderUserId, OfferMessageType type, string content, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var message = new OfferMessage
        {
            OfferConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = type,
            Content = content,
            SentAt = now,
            CreatedAt = now
        };
        await _context.OfferMessages.AddAsync(message, cancellationToken);

        var conv = await _context.OfferConversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conv != null)
        {
            conv.LastMessageAt = now;
            conv.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<ConversationMessagesPageDto> GetMessagesPageAsync(
        int conversationId,
        DateTime? cursorSentAt,
        int take,
        bool olderDirection,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OfferMessages
            .AsNoTracking()
            .Where(m => m.OfferConversationId == conversationId);

        if (cursorSentAt.HasValue)
        {
            query = olderDirection
                ? query.Where(m => m.SentAt < cursorSentAt.Value)
                : query.Where(m => m.SentAt > cursorSentAt.Value);
        }

        query = olderDirection ? query.OrderByDescending(m => m.SentAt) : query.OrderBy(m => m.SentAt);

        var pageSize = Math.Clamp(take, 1, 200);
        var page = await query
            .Take(pageSize + 1)
            .Select(m => new OfferConversationMessageDto
            {
                Id = m.Id,
                Type = m.MessageType,
                SenderUserId = m.SenderUserId,
                SenderDisplayName = m.SenderUser != null
                    ? ((m.SenderUser.FirstName ?? "") + " " + (m.SenderUser.LastName ?? "")).Trim()
                    : null,
                SenderRole = null,
                Content = m.Content,
                SentAt = m.SentAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        if (hasMore) page.RemoveAt(page.Count - 1);

        string? nextCursor = page.Count > 0 ? page[^1].SentAt.ToString("O") : null;

        return new ConversationMessagesPageDto
        {
            Messages = page,
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    public async Task MarkReadAsync(int conversationId, ConversationCaller caller, CancellationToken cancellationToken = default)
    {
        var conv = await _context.OfferConversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conv == null) return;

        var now = DateTime.UtcNow;
        if (caller == ConversationCaller.Teacher)
            conv.TeacherLastReadAt = now;
        else
            conv.StudentLastReadAt = now;
        conv.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConversationParticipantInfo?> ResolveParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.OfferConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                c.Id,
                c.SessionRequestId,
                c.TeacherId,
                c.SessionOfferId,
                TeacherUserId = (int?)c.Teacher.UserId,
                StudentUserId = (int?)c.OpenSessionRequest.RequestedByUserId,
                GuardianUserId = c.OpenSessionRequest.CreatedByGuardian != null
                    ? (int?)c.OpenSessionRequest.CreatedByGuardian.UserId
                    : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return null;

        ConversationCaller? callerRole = null;
        if (row.TeacherUserId == userId) callerRole = ConversationCaller.Teacher;
        else if (row.StudentUserId == userId) callerRole = ConversationCaller.Student;
        else if (row.GuardianUserId == userId) callerRole = ConversationCaller.Student;

        if (callerRole == null) return null;

        return new ConversationParticipantInfo(
            row.Id,
            row.SessionRequestId,
            row.TeacherId,
            row.SessionOfferId,
            row.TeacherUserId ?? 0,
            row.StudentUserId ?? 0,
            row.GuardianUserId,
            callerRole.Value);
    }
}
