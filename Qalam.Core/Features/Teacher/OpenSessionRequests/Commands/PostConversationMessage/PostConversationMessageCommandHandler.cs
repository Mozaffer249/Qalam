using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.PostConversationMessage;

public class PostConversationMessageCommandHandler : ResponseHandler,
    IRequestHandler<PostConversationMessageCommand, Response<OfferConversationMessageDto>>
{
    private readonly IOfferConversationRepository _convRepo;
    private readonly IRabbitMQService _rabbitMq;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PostConversationMessageCommandHandler> _logger;

    public PostConversationMessageCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IOfferConversationRepository convRepo,
        IRabbitMQService rabbitMq,
        UserManager<User> userManager,
        ILogger<PostConversationMessageCommandHandler> logger) : base(localizer)
    {
        _convRepo = convRepo;
        _rabbitMq = rabbitMq;
        _userManager = userManager;
        _logger = logger;
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

        // Email the other party.
        try
        {
            var otherUserId = participant.CallerRole == ConversationCaller.Teacher
                ? participant.StudentUserId
                : participant.TeacherUserId;

            if (otherUserId > 0)
            {
                var user = await _userManager.FindByIdAsync(otherUserId.ToString());
                if (user?.Email != null)
                {
                    await _rabbitMq.QueueEmailAsync(new EmailMessage
                    {
                        To = user.Email,
                        Subject = "رسالة جديدة على محادثة عرضك",
                        Body = "وصلتك رسالة جديدة. افتح المحادثة لقراءتها والرد عليها.",
                        QueuedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to email the other party in conversation {ConversationId}.", request.ConversationId);
        }

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
