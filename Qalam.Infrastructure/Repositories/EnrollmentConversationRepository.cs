using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class EnrollmentConversationRepository : GenericRepositoryAsync<EnrollmentConversation>, IEnrollmentConversationRepository
{
    private readonly ApplicationDBContext _context;

    public EnrollmentConversationRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<EnrollmentConversation?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        return await _context.EnrollmentConversations
            .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId, cancellationToken);
    }

    public async Task<EnrollmentConversation> EnsureExistsAsync(
        int enrollmentId,
        int teacherId,
        int studentUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByEnrollmentIdAsync(enrollmentId, cancellationToken);
        if (existing != null) return existing;

        var now = DateTime.UtcNow;
        var conv = new EnrollmentConversation
        {
            EnrollmentId = enrollmentId,
            TeacherId = teacherId,
            StudentUserId = studentUserId,
            CreatedAt = now
        };
        await _context.EnrollmentConversations.AddAsync(conv, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return conv;
    }

    public async Task<EnrollmentConversationDto?> GetHeaderDtoAsync(
        int conversationId,
        EnrollmentConversationCaller caller,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.EnrollmentConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                c.Id,
                c.EnrollmentId,
                c.LastMessageAt,
                c.StudentLastReadAt,
                c.TeacherLastReadAt,
                TeacherUserId = c.Teacher.UserId,
                TeacherFirstName = c.Teacher.User != null ? c.Teacher.User.FirstName : null,
                TeacherLastName = c.Teacher.User != null ? c.Teacher.User.LastName : null,
                StudentUserId = c.StudentUserId,
                StudentFirstName = c.StudentUser.FirstName,
                StudentLastName = c.StudentUser.LastName,
                UnreadCount = c.Messages.Count(m =>
                    m.SenderUserId != null
                    && (caller == EnrollmentConversationCaller.Teacher
                            ? (c.TeacherLastReadAt == null || m.SentAt > c.TeacherLastReadAt)
                            : (c.StudentLastReadAt == null || m.SentAt > c.StudentLastReadAt)))
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return null;

        var participants = new List<EnrollmentConversationParticipantDto>();
        if (row.TeacherUserId.HasValue)
        {
            participants.Add(new EnrollmentConversationParticipantDto
            {
                UserId = row.TeacherUserId.Value,
                DisplayName = ((row.TeacherFirstName ?? "") + " " + (row.TeacherLastName ?? "")).Trim(),
                Role = "Teacher"
            });
        }

        participants.Add(new EnrollmentConversationParticipantDto
        {
            UserId = row.StudentUserId,
            DisplayName = ((row.StudentFirstName ?? "") + " " + (row.StudentLastName ?? "")).Trim(),
            Role = "Student"
        });

        return new EnrollmentConversationDto
        {
            ConversationId = row.Id,
            EnrollmentId = row.EnrollmentId,
            LastMessageAt = row.LastMessageAt,
            UnreadCount = row.UnreadCount,
            Participants = participants
        };
    }

    public async Task<EnrollmentConversationMessage> AppendMessageAsync(
        int conversationId,
        int? senderUserId,
        EnrollmentMessageType type,
        string content,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var message = new EnrollmentConversationMessage
        {
            EnrollmentConversationId = conversationId,
            SenderUserId = senderUserId,
            MessageType = type,
            Content = content,
            SentAt = now,
            CreatedAt = now
        };
        await _context.EnrollmentConversationMessages.AddAsync(message, cancellationToken);

        var conv = await _context.EnrollmentConversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conv != null)
        {
            conv.LastMessageAt = now;
            conv.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<EnrollmentConversationMessagesPageDto> GetMessagesPageAsync(
        int conversationId,
        DateTime? cursorSentAt,
        int take,
        bool olderDirection,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EnrollmentConversationMessages
            .AsNoTracking()
            .Where(m => m.EnrollmentConversationId == conversationId);

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
            .Select(m => new EnrollmentConversationMessageDto
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

        // Match OfferConversations: older pages are SentAt descending; FE scrolls to end.
        string? nextCursor = page.Count > 0 ? page[^1].SentAt.ToString("O") : null;

        return new EnrollmentConversationMessagesPageDto
        {
            Messages = page,
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    public async Task MarkReadAsync(
        int conversationId,
        EnrollmentConversationCaller caller,
        CancellationToken cancellationToken = default)
    {
        var conv = await _context.EnrollmentConversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conv == null) return;

        var now = DateTime.UtcNow;
        if (caller == EnrollmentConversationCaller.Teacher)
            conv.TeacherLastReadAt = now;
        else
            conv.StudentLastReadAt = now;
        conv.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EnrollmentConversationParticipantInfo?> ResolveParticipantAsync(
        int conversationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.EnrollmentConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                c.Id,
                c.EnrollmentId,
                c.TeacherId,
                TeacherUserId = (int?)c.Teacher.UserId,
                c.StudentUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return null;

        EnrollmentConversationCaller? callerRole = null;
        if (row.TeacherUserId == userId) callerRole = EnrollmentConversationCaller.Teacher;
        else if (row.StudentUserId == userId) callerRole = EnrollmentConversationCaller.Student;

        if (callerRole == null) return null;

        return new EnrollmentConversationParticipantInfo(
            row.Id,
            row.EnrollmentId,
            row.TeacherId,
            row.TeacherUserId ?? 0,
            row.StudentUserId,
            callerRole.Value);
    }
}
