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
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandHandler : ResponseHandler,
    IRequestHandler<CreateSessionOfferCommand, Response<TeacherOfferDetailDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;
    private readonly IOfferConversationService _conversationService;
    private readonly IRabbitMQService _rabbitMq;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<CreateSessionOfferCommandHandler> _logger;

    public CreateSessionOfferCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        IOpenSessionOfferRepository offerRepo,
        IOfferConversationService conversationService,
        IRabbitMQService rabbitMq,
        UserManager<User> userManager,
        ILogger<CreateSessionOfferCommandHandler> logger) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _targetRepo = targetRepo;
        _offerRepo = offerRepo;
        _conversationService = conversationService;
        _rabbitMq = rabbitMq;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Response<TeacherOfferDetailDto>> Handle(
        CreateSessionOfferCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<TeacherOfferDetailDto>("Teacher account not active.");

        var target = await _targetRepo.GetByRequestAndTeacherAsync(request.Data.SessionRequestId, teacher.Id, cancellationToken);
        if (target == null)
            return Forbidden<TeacherOfferDetailDto>("NOT_MATCHED");

        var summary = await _requestRepo.GetStatusSummaryAsync(request.Data.SessionRequestId, cancellationToken);
        if (summary == null)
            return NotFound<TeacherOfferDetailDto>("Request not found.");

        if (summary.Status != OpenSessionRequestStatus.Active && summary.Status != OpenSessionRequestStatus.ReceivingOffers)
            return Conflict<TeacherOfferDetailDto>("REQUEST_NOT_ACTIVE");

        var existing = await _offerRepo.GetExistingActiveOfferAsync(request.Data.SessionRequestId, teacher.Id, cancellationToken);
        if (existing != null)
        {
            return Conflict<TeacherOfferDetailDto>(
                "DUPLICATE_OFFER",
                Meta: new DuplicateOfferMetaDto
                {
                    ExistingOfferId = existing.Value.OfferId,
                    ExistingOfferStatus = existing.Value.Status
                });
        }

        var now = DateTime.UtcNow;
        var offer = new OpenSessionOffer
        {
            SessionRequestId = request.Data.SessionRequestId,
            TeacherId = teacher.Id,
            Price = request.Data.Price,
            TeacherNotes = request.Data.TeacherNotes,
            Status = OpenSessionOfferStatus.Pending,
            Version = 1,
            ExpiresAt = now.AddHours(request.Data.ValidityHours),
            CreatedAt = now
        };

        await _offerRepo.AddAsync(offer);
        await _offerRepo.SaveChangesAsync();

        // Flip the target row to OfferSubmitted (idempotent).
        await _targetRepo.SetStatusAsync(request.Data.SessionRequestId, teacher.Id, OpenSessionRequestTargetStatus.OfferSubmitted, cancellationToken);

        // First offer on this request? Flip request status Active → ReceivingOffers.
        if (summary.Status == OpenSessionRequestStatus.Active)
        {
            await _requestRepo.UpdateStatusAsync(request.Data.SessionRequestId, OpenSessionRequestStatus.ReceivingOffers, cancellationToken);
        }

        // Conversation: post the "تم تقديم العرض" system message + set offer pointer.
        try
        {
            await _conversationService.RecordOfferLifecycleEventAsync(
                request.Data.SessionRequestId,
                teacher.Id,
                offer.Id,
                OfferMessageType.System,
                "تم تقديم العرض",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post system message for new offer {OfferId}.", offer.Id);
        }

        // Email the requester (could be the student or the guardian — RequestedByUserId is whoever submitted).
        await TryNotifyRequesterAsync(summary.RequestedByUserId, summary.CreatedByGuardianId);

        var detail = await _offerRepo.GetTeacherDetailDtoAsync(offer.Id, teacher.Id, cancellationToken);
        return Created(entity: detail!);
    }

    private async Task TryNotifyRequesterAsync(int requestedByUserId, int? createdByGuardianId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(requestedByUserId.ToString());
            if (user?.Email == null) return;

            await _rabbitMq.QueueEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = "عرض جديد على طلب جلساتك",
                Body = "وصلك عرض جديد من معلم. افتح قائمة \"العروض\" لمراجعة التفاصيل.",
                QueuedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to email requester {UserId} about a new offer.", requestedByUserId);
        }
    }
}
