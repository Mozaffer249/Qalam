using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.WithdrawSessionOffer;

public class WithdrawSessionOfferCommandHandler : ResponseHandler,
    IRequestHandler<WithdrawSessionOfferCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;
    private readonly IOfferConversationService _conversationService;
    private readonly IRabbitMQService _rabbitMq;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<WithdrawSessionOfferCommandHandler> _logger;

    public WithdrawSessionOfferCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionOfferRepository offerRepo,
        IOfferConversationService conversationService,
        IRabbitMQService rabbitMq,
        UserManager<User> userManager,
        ILogger<WithdrawSessionOfferCommandHandler> logger) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _offerRepo = offerRepo;
        _conversationService = conversationService;
        _rabbitMq = rabbitMq;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(WithdrawSessionOfferCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<string>("Teacher account not active.");

        var offer = await _offerRepo.GetByIdForOwnerActionAsync(request.OfferId, teacher.Id, cancellationToken);
        if (offer == null)
            return NotFound<string>("Offer not found.");
        if (offer.Status != OpenSessionOfferStatus.Pending)
            return Conflict<string>("OFFER_NOT_PENDING");

        var now = DateTime.UtcNow;
        offer.Status = OpenSessionOfferStatus.Withdrawn;
        offer.WithdrawnAt = now;
        if (!string.IsNullOrWhiteSpace(request.Data.Reason))
            offer.RejectionReason = request.Data.Reason;
        offer.UpdatedAt = now;

        await _offerRepo.UpdateAsync(offer);
        await _offerRepo.SaveChangesAsync();

        try
        {
            await _conversationService.RecordOfferLifecycleEventAsync(
                offer.SessionRequestId,
                teacher.Id,
                sessionOfferId: null,
                OfferMessageType.System,
                "تم سحب العرض",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post withdraw system message for offer {OfferId}.", offer.Id);
        }

        var summary = await _requestRepo.GetStatusSummaryAsync(offer.SessionRequestId, cancellationToken);
        if (summary != null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(summary.RequestedByUserId.ToString());
                if (user?.Email != null)
                {
                    await _rabbitMq.QueueEmailAsync(new EmailMessage
                    {
                        To = user.Email,
                        Subject = "تم سحب عرض على طلب جلساتك",
                        Body = "قام أحد المعلمين بسحب عرضه. افتح قائمة العروض لرؤية ما تبقى من عروض.",
                        QueuedAt = now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to email requester about offer withdraw for offer {OfferId}.", offer.Id);
            }
        }

        return Success(entity: "تم سحب العرض");
    }
}
