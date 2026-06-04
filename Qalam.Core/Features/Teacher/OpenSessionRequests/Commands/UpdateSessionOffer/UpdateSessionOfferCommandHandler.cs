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

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.UpdateSessionOffer;

public class UpdateSessionOfferCommandHandler : ResponseHandler,
    IRequestHandler<UpdateSessionOfferCommand, Response<TeacherOfferDetailDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;
    private readonly IOfferConversationService _conversationService;
    private readonly IRabbitMQService _rabbitMq;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UpdateSessionOfferCommandHandler> _logger;

    public UpdateSessionOfferCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionOfferRepository offerRepo,
        IOfferConversationService conversationService,
        IRabbitMQService rabbitMq,
        UserManager<User> userManager,
        ILogger<UpdateSessionOfferCommandHandler> logger) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _offerRepo = offerRepo;
        _conversationService = conversationService;
        _rabbitMq = rabbitMq;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Response<TeacherOfferDetailDto>> Handle(
        UpdateSessionOfferCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<TeacherOfferDetailDto>("Teacher account not active.");

        var offer = await _offerRepo.GetByIdForOwnerActionAsync(request.OfferId, teacher.Id, cancellationToken);
        if (offer == null)
            return NotFound<TeacherOfferDetailDto>("Offer not found.");
        if (offer.Status != OpenSessionOfferStatus.Pending)
            return Conflict<TeacherOfferDetailDto>("OFFER_NOT_PENDING");

        var now = DateTime.UtcNow;
        if (request.Data.Price.HasValue) offer.Price = request.Data.Price.Value;
        if (request.Data.TeacherNotes != null) offer.TeacherNotes = request.Data.TeacherNotes;
        if (request.Data.ValidityHours.HasValue) offer.ExpiresAt = now.AddHours(request.Data.ValidityHours.Value);
        offer.Version += 1;
        offer.UpdatedAt = now;

        await _offerRepo.UpdateAsync(offer);
        await _offerRepo.SaveChangesAsync();

        var systemMessage = request.Data.Price.HasValue
            ? $"تم تحديث العرض - السعر الجديد: {offer.Price} ر.س"
            : "تم تحديث العرض";

        try
        {
            await _conversationService.RecordOfferLifecycleEventAsync(
                offer.SessionRequestId,
                teacher.Id,
                offer.Id,
                OfferMessageType.OfferUpdate,
                systemMessage,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post update system message for offer {OfferId}.", offer.Id);
        }

        // Notify the requester.
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
                        Subject = "تحديث على عرض معلم",
                        Body = systemMessage,
                        QueuedAt = now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to email requester about offer update for offer {OfferId}.", offer.Id);
            }
        }

        var detail = await _offerRepo.GetTeacherDetailDtoAsync(offer.Id, teacher.Id, cancellationToken);
        return Success(entity: detail!);
    }
}
