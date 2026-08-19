using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandHandler : ResponseHandler,
    IRequestHandler<CreateSessionOfferCommand, Response<TeacherOfferDetailDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;
    private readonly ISessionAvailabilityMatchService _availabilityMatch;
    private readonly IOfferConversationService _conversationService;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingSnapshotWriter _pricingSnapshotWriter;
    private readonly IRabbitMQService _rabbitMq;
    private readonly UserManager<User> _userManager;
    private readonly OpenSessionRequestSettings _osrSettings;
    private readonly ILogger<CreateSessionOfferCommandHandler> _logger;

    public CreateSessionOfferCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        IOpenSessionOfferRepository offerRepo,
        ISessionAvailabilityMatchService availabilityMatch,
        IOfferConversationService conversationService,
        IPricingEngine pricingEngine,
        IPricingSnapshotWriter pricingSnapshotWriter,
        IRabbitMQService rabbitMq,
        UserManager<User> userManager,
        IOptions<OpenSessionRequestSettings> osrSettings,
        ILogger<CreateSessionOfferCommandHandler> logger) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _targetRepo = targetRepo;
        _offerRepo = offerRepo;
        _availabilityMatch = availabilityMatch;
        _conversationService = conversationService;
        _pricingEngine = pricingEngine;
        _pricingSnapshotWriter = pricingSnapshotWriter;
        _rabbitMq = rabbitMq;
        _userManager = userManager;
        _osrSettings = osrSettings.Value;
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

        var match = await _availabilityMatch.MatchAsync(
            teacher.Id, request.Data.SessionRequestId, cancellationToken);
        var blocked = match
            .Where(m => m.Status != SessionAvailabilityStatus.Available)
            .ToList();
        if (blocked.Count > 0)
        {
            var hasPast = blocked.Any(m => m.Status == SessionAvailabilityStatus.Past);
            var hasScheduleConflict = blocked.Any(m => m.Status == SessionAvailabilityStatus.Conflict);
            var code = hasPast
                ? "SESSION_DATE_PAST"
                : hasScheduleConflict
                    ? "SCHEDULE_CONFLICT"
                    : "OUTSIDE_AVAILABILITY";
            return Conflict<TeacherOfferDetailDto>(
                code,
                Meta: new OfferAvailabilityBlockMetaDto
                {
                    Sessions = blocked.Select(m => new OfferAvailabilityBlockSessionDto
                    {
                        SessionId = m.SessionId,
                        SequenceNumber = m.SequenceNumber,
                        Status = m.Status,
                        ConflictWith = m.ConflictWith,
                    }).ToList(),
                });
        }

        var now = DateTime.UtcNow;
        var requestExpiresAt = await _requestRepo.GetExpiresAtAsync(request.Data.SessionRequestId, cancellationToken);

        var osr = await _requestRepo.GetByIdAsync(request.Data.SessionRequestId);
        if (osr == null)
            return NotFound<TeacherOfferDetailDto>("Request not found.");

        var scheduleSlots = await _requestRepo.GetSessionScheduleSlotsAsync(
            request.Data.SessionRequestId, cancellationToken);
        var totalMinutes = scheduleSlots.Sum(s => s.DurationMinutes);
        if (totalMinutes <= 0)
            return BadRequest<TeacherOfferDetailDto>("Total session duration must be greater than zero.");

        var sessionTypeCode = osr.GroupType.HasValue ? "group" : "individual";
        var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = osr.DomainId,
            SessionTypeCode = sessionTypeCode,
            TotalMinutes = totalMinutes,
            TeacherId = teacher.Id
        }, cancellationToken);

        var offer = new OpenSessionOffer
        {
            SessionRequestId = request.Data.SessionRequestId,
            TeacherId = teacher.Id,
            Price = estimate.TotalPrice,
            TeacherNotes = request.Data.TeacherNotes,
            Status = OpenSessionOfferStatus.Pending,
            Version = 1,
            ExpiresAt = OpenSessionRequestExpiry.ResolveOfferExpiry(
                now, _osrSettings.DefaultOfferValidityHours, requestExpiresAt),
            CreatedAt = now
        };

        await _offerRepo.AddAsync(offer);
        await _offerRepo.SaveChangesAsync();

        var snapshot = await _pricingSnapshotWriter.CreateAndSaveAsync(new CreatePricingSnapshotRequest
        {
            Context = PricingSnapshotContext.OpenSessionOffer,
            ContextEntityId = offer.Id,
            DomainId = osr.DomainId,
            SessionTypeCode = sessionTypeCode,
            TotalMinutes = totalMinutes,
            TeacherId = teacher.Id
        }, cancellationToken);

        offer.PricingSnapshotId = snapshot.Id;
        await _offerRepo.UpdateAsync(offer);
        await _offerRepo.SaveChangesAsync();

        await _targetRepo.SetStatusAsync(request.Data.SessionRequestId, teacher.Id, OpenSessionRequestTargetStatus.OfferSubmitted, cancellationToken);

        if (summary.Status == OpenSessionRequestStatus.Active)
        {
            await _requestRepo.UpdateStatusAsync(request.Data.SessionRequestId, OpenSessionRequestStatus.ReceivingOffers, cancellationToken);
        }

        var isOfferScoped = summary.TargetedTeacherId == null;
        try
        {
            await _conversationService.RecordOfferLifecycleEventAsync(
                request.Data.SessionRequestId,
                teacher.Id,
                offer.Id,
                isOfferScoped,
                OfferMessageType.System,
                "تم تقديم العرض",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post system message for new offer {OfferId}.", offer.Id);
        }

        await TryNotifyRequesterAsync(summary.RequestedByUserId);

        var detail = await _offerRepo.GetTeacherDetailDtoAsync(offer.Id, teacher.Id, cancellationToken);
        return Created(entity: detail!);
    }

    private async Task TryNotifyRequesterAsync(int requestedByUserId)
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
