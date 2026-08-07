using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.PostConversationMessage;

public class PostConversationMessageCommandHandler : ResponseHandler,
    IRequestHandler<PostConversationMessageCommand, Response<OfferConversationMessageDto>>
{
    private readonly IOfferConversationRepository _convRepo;
    private readonly IChatEmailNotifier _chatEmail;
    private readonly UserManager<User> _userManager;

    public PostConversationMessageCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IOfferConversationRepository convRepo,
        IChatEmailNotifier chatEmail,
        UserManager<User> userManager) : base(localizer)
    {
        _convRepo = convRepo;
        _chatEmail = chatEmail;
        _userManager = userManager;
    }

    public async Task<Response<OfferConversationMessageDto>> Handle(
        PostConversationMessageCommand request,
        CancellationToken cancellationToken)
    {
        var participant = await _convRepo.ResolveParticipantAsync(request.ConversationId, request.UserId, cancellationToken);
        if (participant == null)
            return Forbidden<OfferConversationMessageDto>("NOT_A_PARTICIPANT");

        var message = await _convRepo.AppendMessageAsync(
            request.ConversationId,
            senderUserId: request.UserId,
            OfferMessageType.Text,
            request.Data.Content,
            cancellationToken);

        var otherUserId = participant.CallerRole == ConversationCaller.Teacher
            ? participant.StudentUserId
            : participant.TeacherUserId;

        await _chatEmail.TryNotifyAsync(
            request.ConversationId,
            otherUserId,
            subject: "رسالة جديدة على محادثة عرضك",
            body: "وصلتك رسالة جديدة. افتح المحادثة لقراءتها والرد عليها.",
            cancellationToken);

        var sender = await _userManager.FindByIdAsync(request.UserId.ToString());
        var dto = new OfferConversationMessageDto
        {
            Id = message.Id,
            Type = message.MessageType,
            SenderUserId = message.SenderUserId,
            SenderDisplayName = sender != null
                ? ((sender.FirstName ?? "") + " " + (sender.LastName ?? "")).Trim()
                : null,
            SenderRole = participant.CallerRole == ConversationCaller.Teacher ? "Teacher" : "Student",
            Content = message.Content,
            SentAt = message.SentAt
        };

        return Created(entity: dto);
    }
}
