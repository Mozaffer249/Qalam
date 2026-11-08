using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Conversations;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Conversations.Queries.GetMyConversations;

public class GetMyConversationsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyConversationsQuery, Response<List<MyConversationListItemDto>>>
{
    private readonly IOfferConversationRepository _offerRepo;
    private readonly IEnrollmentConversationRepository _enrollmentRepo;

    public GetMyConversationsQueryHandler(
        IOfferConversationRepository offerRepo,
        IEnrollmentConversationRepository enrollmentRepo,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _offerRepo = offerRepo;
        _enrollmentRepo = enrollmentRepo;
    }

    public async Task<Response<List<MyConversationListItemDto>>> Handle(
        GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var offerRows = await _offerRepo.ListByUserAsync(userId, cancellationToken);
        var enrollmentRows = await _enrollmentRepo.ListByUserAsync(userId, cancellationToken);

        var items = new List<MyConversationListItemDto>();

        foreach (var r in offerRows)
        {
            var participants = new List<ConversationParticipantDto>();
            if (r.TeacherUserId > 0)
                participants.Add(new ConversationParticipantDto
                {
                    UserId = r.TeacherUserId,
                    DisplayName = r.TeacherName,
                    Role = "Teacher"
                });
            if (r.StudentUserId > 0)
                participants.Add(new ConversationParticipantDto
                {
                    UserId = r.StudentUserId,
                    Role = "Student"
                });

            items.Add(new MyConversationListItemDto
            {
                ConversationId = r.ConversationId,
                ConversationType = "Offer",
                TeacherName = r.TeacherName,
                SubjectName = r.SubjectName,
                LastMessageContent = Truncate(r.LastMessageContent, 120),
                LastMessageAt = r.LastMessageAt,
                UnreadCount = r.UnreadCount,
                OfferId = r.OfferId,
                Participants = participants
            });
        }

        foreach (var r in enrollmentRows)
        {
            var participants = new List<ConversationParticipantDto>();
            if (r.TeacherUserId > 0)
                participants.Add(new ConversationParticipantDto
                {
                    UserId = r.TeacherUserId,
                    DisplayName = r.TeacherName,
                    Role = "Teacher"
                });
            if (r.StudentUserId > 0)
                participants.Add(new ConversationParticipantDto
                {
                    UserId = r.StudentUserId,
                    Role = "Student"
                });

            items.Add(new MyConversationListItemDto
            {
                ConversationId = r.ConversationId,
                ConversationType = "Enrollment",
                TeacherName = r.TeacherName,
                SubjectName = r.SubjectName,
                LastMessageContent = Truncate(r.LastMessageContent, 120),
                LastMessageAt = r.LastMessageAt,
                UnreadCount = r.UnreadCount,
                EnrollmentId = r.EnrollmentId,
                Participants = participants
            });
        }

        items.Sort((a, b) =>
        {
            var aAt = a.LastMessageAt ?? DateTime.MinValue;
            var bAt = b.LastMessageAt ?? DateTime.MinValue;
            return bAt.CompareTo(aAt);
        });

        return Success(entity: items);
    }

    private static string? Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
