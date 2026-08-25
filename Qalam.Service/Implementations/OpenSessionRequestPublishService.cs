using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class OpenSessionRequestPublishService : IOpenSessionRequestPublishService
{
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IEducationDomainRepository _domainRepo;
    private readonly ITargetedOpenSessionRequestValidator _targetedValidator;
    private readonly IGuardianChildrenService _guardianChildren;
    private readonly IOpenSessionRequestTargetingService _targetingService;
    private readonly ITargetedOpenSessionRequestPricingService _targetedPricing;
    private readonly OpenSessionRequestSettings _osrSettings;

    public OpenSessionRequestPublishService(
        IOpenSessionRequestRepository requestRepo,
        IEducationDomainRepository domainRepo,
        ITargetedOpenSessionRequestValidator targetedValidator,
        IGuardianChildrenService guardianChildren,
        IOpenSessionRequestTargetingService targetingService,
        ITargetedOpenSessionRequestPricingService targetedPricing,
        IOptions<OpenSessionRequestSettings> osrSettings)
    {
        _requestRepo = requestRepo;
        _domainRepo = domainRepo;
        _targetedValidator = targetedValidator;
        _guardianChildren = guardianChildren;
        _targetingService = targetingService;
        _targetedPricing = targetedPricing;
        _osrSettings = osrSettings.Value;
    }

    public async Task<OpenSessionRequestPublishResultDto> PublishAsync(
        int requestId,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _requestRepo.GetForPublishAsync(requestId, cancellationToken);
        if (entity is null)
            return OpenSessionRequestPublishResultDto.Fail(
                OpenSessionRequestPublishFailureKind.NotFound, "الطلب غير موجود");

        if (entity.Status != OpenSessionRequestStatus.Draft)
            return OpenSessionRequestPublishResultDto.Fail(
                OpenSessionRequestPublishFailureKind.BadRequest, "يمكن نشر المسودات فقط.");

        if (entity.Sessions.Count == 0)
            return OpenSessionRequestPublishResultDto.Fail(
                OpenSessionRequestPublishFailureKind.BadRequest, "أضف جلسة واحدةً واحدة قبل النشر.");

        if (entity.TotalSessionsCount != entity.Sessions.Count)
            return OpenSessionRequestPublishResultDto.Fail(
                OpenSessionRequestPublishFailureKind.BadRequest, "عدد الجلسات غير متطابق.");

        var domain = await _domainRepo.GetDomainDtoByIdAsync(entity.DomainId);
        if (QuranDomainHelper.IsQuranDomain(domain?.Code, domain?.NameEn)
            && entity.Sessions.Any(s => !s.QuranContentTypeId.HasValue || !s.QuranLevelId.HasValue))
        {
            return OpenSessionRequestPublishResultDto.Fail(
                OpenSessionRequestPublishFailureKind.BadRequest,
                "جلسات مجال القرآن تتطلب QuranContentTypeId و QuranLevelId");
        }

        if (entity.TargetedTeacherId.HasValue)
        {
            var sessionDtos = MapSessionsToCreateDtos(entity);
            var err = await _targetedValidator.ValidateAsync(
                entity.TargetedTeacherId.Value, entity.SubjectId, sessionDtos, cancellationToken);
            if (err is not null)
                return OpenSessionRequestPublishResultDto.Fail(
                    OpenSessionRequestPublishFailureKind.BadRequest, err);
        }

        var now = DateTime.UtcNow;
        var isTargeted = entity.TargetedTeacherId.HasValue;
        var firstSessionStartUtc = OpenSessionRequestExpiry.FirstSessionStartUtc(
            entity.Sessions.Select(s => (
                s.PreferredDate,
                s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.StartTime : null)));

        var leadError = ValidateMinimumLead(now, firstSessionStartUtc, isTargeted);
        if (leadError != null)
            return OpenSessionRequestPublishResultDto.Fail(
                OpenSessionRequestPublishFailureKind.BadRequest, leadError);

        var ownedStudentIds = await _guardianChildren.GetOwnedStudentIdsAsync(
            actingUserId, cancellationToken);
        foreach (var invite in entity.Invitations)
        {
            if (invite.Status == OpenSessionRequestInvitationStatus.Pending
                && ownedStudentIds.Contains(invite.InvitedStudentId))
            {
                invite.Status = OpenSessionRequestInvitationStatus.Accepted;
                invite.RespondedAt = now;
            }
        }

        var hasExternalPending = entity.Invitations.Any(i =>
            i.Status == OpenSessionRequestInvitationStatus.Pending);
        var status = hasExternalPending
            ? OpenSessionRequestStatus.PendingInvitations
            : OpenSessionRequestStatus.Active;

        entity.Status = status;
        entity.PublishedAt = now;
        entity.ExpiresAt = OpenSessionRequestExpiry.ResolveRequestExpiry(
            now, entity.ExpiresAt, firstSessionStartUtc, _osrSettings, isTargeted);

        await _requestRepo.SaveChangesAsync();

        if (entity.TargetedTeacherId.HasValue)
        {
            await _targetedPricing.FreezeIfNeededAsync(entity, actingUserId, cancellationToken);
        }

        if (status == OpenSessionRequestStatus.Active)
        {
            if (entity.TargetedTeacherId.HasValue)
            {
                await _targetingService.NotifyTargetedTeacherAsync(
                    entity.Id, entity.TargetedTeacherId.Value, cancellationToken);
            }
            else
            {
                await _targetingService.RunMatchingAndNotifyAsync(entity.Id, cancellationToken);
            }
        }

        return OpenSessionRequestPublishResultDto.Success(entity.Id);
    }

    private static List<CreateOpenSessionRequestSessionDto> MapSessionsToCreateDtos(
        OpenSessionRequest entity) =>
        entity.Sessions.Select(s => new CreateOpenSessionRequestSessionDto
        {
            SequenceNumber = s.SequenceNumber,
            PreferredDate = s.PreferredDate ?? default,
            TimeSlotId = s.TimeSlotId ?? 0,
            DurationMinutes = s.DurationMinutes,
            QuranContentTypeId = s.QuranContentTypeId,
            QuranLevelId = s.QuranLevelId,
            Notes = s.Notes,
            Units = s.Units.Select(u => new CreateOpenSessionRequestUnitDto
            {
                ContentUnitId = u.ContentUnitId,
                LessonId = u.LessonId,
                CustomUnitLabel = u.CustomUnitLabel,
                IncludesAllLessons = u.IncludesAllLessons,
            }).ToList(),
        }).ToList();

    private string? ValidateMinimumLead(
        DateTime nowUtc,
        DateTime? firstSessionStartUtc,
        bool isTargeted)
    {
        if (firstSessionStartUtc == null)
            return "يجب تحديد تاريخ ووقت للجلسة الأولى";

        var leadHours = OpenSessionRequestExpiry.MinimumLeadHours(_osrSettings, isTargeted);
        var earliestAllowed = nowUtc.AddHours(Math.Max(0, leadHours));
        if (firstSessionStartUtc.Value < earliestAllowed)
        {
            return isTargeted
                ? $"للطلب الموجَّه يجب أن تكون الجلسة الأولى بعد {leadHours} ساعة على الأقل من الآن"
                : $"للطلب المنشور يجب أن تكون الجلسة الأولى بعد {leadHours} ساعة على الأقل من الآن";
        }

        return null;
    }
}
