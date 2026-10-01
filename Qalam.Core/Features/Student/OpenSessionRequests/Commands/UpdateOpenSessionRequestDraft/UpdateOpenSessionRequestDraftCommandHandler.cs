using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Service;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.UpdateOpenSessionRequestDraft;

public class UpdateOpenSessionRequestDraftCommandHandler
    : ResponseHandler, IRequestHandler<UpdateOpenSessionRequestDraftCommand, Response<OpenSessionRequestDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly ITargetedOpenSessionRequestValidator _targetedValidator;
    private readonly OpenSessionRequestSettings _osrSettings;
    private readonly IMapper _mapper;
    private readonly IOpenSessionRequestStudentPricingEnricher _pricingEnricher;
    private readonly IGuardianChildrenService _guardianChildren;
    private readonly ITargetedOpenSessionRequestPricingService _targetedPricing;
    private readonly IOpenSessionRequestTargetingService _targetingService;

    public const string RequestEditedReason = "REQUEST_EDITED";

    private static readonly OpenSessionRequestStatus[] EditableStatuses =
    {
        OpenSessionRequestStatus.Draft,
        OpenSessionRequestStatus.PendingInvitations,
        OpenSessionRequestStatus.Active,
        OpenSessionRequestStatus.ReceivingOffers,
    };

    /// <summary>Closed requests the owner may reopen with edited timing.</summary>
    private static readonly OpenSessionRequestStatus[] RepublishableStatuses =
    {
        OpenSessionRequestStatus.Expired,
        OpenSessionRequestStatus.Cancelled,
        OpenSessionRequestStatus.Rejected,
    };

    public UpdateOpenSessionRequestDraftCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        ITargetedOpenSessionRequestValidator targetedValidator,
        IOptions<OpenSessionRequestSettings> osrSettings,
        IMapper mapper,
        IOpenSessionRequestStudentPricingEnricher pricingEnricher,
        IGuardianChildrenService guardianChildren,
        ITargetedOpenSessionRequestPricingService targetedPricing,
        IOpenSessionRequestTargetingService targetingService) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _targetedValidator = targetedValidator;
        _osrSettings = osrSettings.Value;
        _mapper = mapper;
        _pricingEnricher = pricingEnricher;
        _guardianChildren = guardianChildren;
        _targetedPricing = targetedPricing;
        _targetingService = targetingService;
    }

    public async Task<Response<OpenSessionRequestDetailDto>> Handle(
        UpdateOpenSessionRequestDraftCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.OpenSessionRequests
            .Include(r => r.Sessions).ThenInclude(s => s.Units)
            .Include(r => r.Invitations)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (entity == null)
            return NotFound<OpenSessionRequestDetailDto>("الطلب غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken))
            return Unauthorized<OpenSessionRequestDetailDto>("Forbidden");

        var isRepublish = RepublishableStatuses.Contains(entity.Status);
        if (!isRepublish && !EditableStatuses.Contains(entity.Status))
            return BadRequest<OpenSessionRequestDetailDto>("REQUEST_NOT_EDITABLE");

        var isPublished = entity.Status != OpenSessionRequestStatus.Draft;
        var previousStatus = entity.Status;

        if (isPublished && request.Data.TargetedTeacherId != entity.TargetedTeacherId)
            return BadRequest<OpenSessionRequestDetailDto>("TARGETED_TEACHER_LOCKED");

        var data = request.Data;
        var access = await _accessGuard.CanCreateForStudentAsync(request.UserId, data.StudentId, cancellationToken);
        if (!access.Allowed)
            return Unauthorized<OpenSessionRequestDetailDto>(access.Reason ?? "Forbidden");

        if (!await _db.EducationDomains.AnyAsync(x => x.Id == data.DomainId, cancellationToken))
            return NotFound<OpenSessionRequestDetailDto>("المجال غير موجود");
        if (!await _db.Subjects.AnyAsync(x => x.Id == data.SubjectId, cancellationToken))
            return NotFound<OpenSessionRequestDetailDto>("المادة غير موجودة");
        if (!await _db.TeachingModes.AnyAsync(x => x.Id == data.TeachingModeId, cancellationToken))
            return NotFound<OpenSessionRequestDetailDto>("طريقة التدريس غير موجودة");

        var domain = await _db.EducationDomains
            .Where(x => x.Id == data.DomainId)
            .Select(x => new { x.Code, x.NameEn })
            .FirstOrDefaultAsync(cancellationToken);
        var isQuranDomain = QuranDomainHelper.IsQuranDomain(domain?.Code, domain?.NameEn);
        if (isQuranDomain
            && data.Sessions.Any(s => !s.QuranContentTypeId.HasValue || !s.QuranLevelId.HasValue))
            return BadRequest<OpenSessionRequestDetailDto>("جلسات مجال القرآن تتطلب QuranContentTypeId و QuranLevelId");

        if (data.TargetedTeacherId.HasValue)
        {
            var err = await _targetedValidator.ValidateAsync(
                data.TargetedTeacherId.Value, data.SubjectId, data.Sessions, cancellationToken);
            if (err is not null)
                return BadRequest<OpenSessionRequestDetailDto>(err);
        }

        if (data.TotalSessionsCount != data.Sessions.Count)
            return BadRequest<OpenSessionRequestDetailDto>("totalSessionsCount يجب أن يطابق عدد الجلسات");

        entity.StudentId = data.StudentId;
        entity.CreatedByGuardianId = access.GuardianId;
        entity.DomainId = data.DomainId;
        entity.CurriculumId = data.CurriculumId;
        entity.LevelId = data.LevelId;
        entity.GradeId = data.GradeId;
        entity.TermId = data.TermId;
        entity.UniversityId = data.UniversityId;
        entity.CollegeId = data.CollegeId;
        entity.DepartmentId = data.DepartmentId;
        entity.AcademicProgramId = data.AcademicProgramId;
        entity.SubjectId = data.SubjectId;
        entity.TeachingModeId = data.TeachingModeId;
        entity.TargetedTeacherId = data.TargetedTeacherId;
        entity.GroupType = data.GroupType;
        entity.TotalSessionsCount = data.TotalSessionsCount;
        entity.StudentNotes = data.StudentNotes;

        _db.RemoveRange(entity.Sessions.SelectMany(s => s.Units));
        _db.RemoveRange(entity.Sessions);
        entity.Sessions.Clear();
        if (!isPublished)
        {
            _db.RemoveRange(entity.Invitations);
            entity.Invitations.Clear();
        }

        foreach (var s in data.Sessions)
        {
            var session = new OpenSessionRequestSession
            {
                SequenceNumber = s.SequenceNumber,
                PreferredDate = s.PreferredDate,
                TimeSlotId = s.TimeSlotId,
                DurationMinutes = s.DurationMinutes,
                // Non-Quran clients may send an education level id here (FK is QuranLevels).
                QuranContentTypeId = isQuranDomain ? s.QuranContentTypeId : null,
                QuranLevelId = isQuranDomain ? s.QuranLevelId : null,
                Notes = s.Notes,
            };
            foreach (var u in s.Units)
                session.Units.Add(new OpenSessionRequestSessionUnit
                {
                    ContentUnitId = u.ContentUnitId,
                    LessonId = u.LessonId,
                    CustomUnitLabel = string.IsNullOrWhiteSpace(u.CustomUnitLabel) ? null : u.CustomUnitLabel.Trim(),
                    IncludesAllLessons = u.IncludesAllLessons,
                });
            entity.Sessions.Add(session);
        }

        var now = DateTime.UtcNow;
        var isTargeted = data.TargetedTeacherId.HasValue;

        if (isPublished)
        {
            var ownedStudentIds = await _guardianChildren.GetOwnedStudentIdsAsync(
                request.UserId, cancellationToken);
            MergePublishedInvitations(entity, data, ownedStudentIds, now);
        }
        else
        {
            foreach (var invitedId in data.InvitedStudentIds.Distinct())
            {
                entity.Invitations.Add(new OpenSessionRequestInvitation
                {
                    InvitedStudentId = invitedId,
                    InvitedByStudentId = data.StudentId,
                    Status = OpenSessionRequestInvitationStatus.Pending,
                });
            }
        }

        // Recompute expiry from (possibly moved) session dates; drafts skip min-lead until publish.
        var firstSessionStartUtc = await OpenSessionRequestDeadlineResolver
            .ResolveFirstSessionStartUtcFromDtosAsync(_db, data.Sessions, cancellationToken);
        if (isPublished)
        {
            var leadError = OpenSessionRequestDeadlineResolver.ValidateMinimumLead(
                now, firstSessionStartUtc, _osrSettings, isTargeted);
            if (leadError != null)
                return BadRequest<OpenSessionRequestDetailDto>(leadError);
        }
        if (isRepublish)
        {
            entity.CancelledAt = null;
            entity.CancellationReason = null;
            entity.PublishedAt = now;
            entity.ExpiryNudgeStage = 0;
        }
        // Window is anchored to publish time; the old ExpiresAt may already be capped by the previous first session.
        var windowBound = data.ExpiresAt
            ?? (isPublished && entity.PublishedAt.HasValue
                ? entity.PublishedAt.Value.AddDays(Math.Max(1, _osrSettings.RequestWindowDays))
                : null);
        entity.ExpiresAt = OpenSessionRequestDeadlineResolver.ResolveExpiry(
            now,
            windowBound,
            firstSessionStartUtc,
            _osrSettings,
            isTargeted);

        var editedTeacherIds = new List<int>();
        if (isPublished)
        {
            editedTeacherIds = await AutoRejectPendingOffersAsync(entity.Id, now, cancellationToken);

            var hasPendingInvites = entity.Invitations.Any(i =>
                i.Status == OpenSessionRequestInvitationStatus.Pending);
            entity.Status = (isRepublish || previousStatus == OpenSessionRequestStatus.PendingInvitations)
                            && hasPendingInvites
                ? OpenSessionRequestStatus.PendingInvitations
                : OpenSessionRequestStatus.Active;
        }

        var republishedTeacherIds = isRepublish
            ? await ResetTargetsForRepublishAsync(entity.Id, now, cancellationToken)
            : new List<int>();

        var staleSnapshot = isPublished && isTargeted && entity.PricingSnapshotId.HasValue
            ? await _db.PricingSnapshots.FirstOrDefaultAsync(
                s => s.Id == entity.PricingSnapshotId, cancellationToken)
            : null;
        if (staleSnapshot != null)
            entity.PricingSnapshotId = null;

        await _db.SaveChangesAsync(cancellationToken);

        if (isPublished)
        {
            if (staleSnapshot != null)
            {
                _db.PricingSnapshots.Remove(staleSnapshot);
                await _db.SaveChangesAsync(cancellationToken);
            }
            if (isTargeted)
                await _targetedPricing.FreezeIfNeededAsync(entity, request.UserId, cancellationToken);

            if (isRepublish)
                await DispatchAfterRepublishAsync(entity, republishedTeacherIds, cancellationToken);
            else
                await DispatchAfterEditAsync(entity, previousStatus, editedTeacherIds, cancellationToken);
        }

        var detail = await _db.OpenSessionRequests
            .AsNoTracking()
            .Include(r => r.Student).ThenInclude(s => s!.User)
            .Include(r => r.CreatedByGuardian).ThenInclude(g => g!.User)
            .Include(r => r.Domain)
            .Include(r => r.Curriculum)
            .Include(r => r.Level)
            .Include(r => r.Grade)
            .Include(r => r.Term)
            .Include(r => r.University)
            .Include(r => r.College)
            .Include(r => r.Department)
            .Include(r => r.AcademicProgram)
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.Sessions).ThenInclude(s => s.QuranContentType)
            .Include(r => r.Sessions).ThenInclude(s => s.QuranLevel)
            .Include(r => r.Sessions).ThenInclude(s => s.TimeSlot)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .Include(r => r.Offers)
            .Include(r => r.PricingSnapshot)
            .FirstAsync(r => r.Id == entity.Id, cancellationToken);

        var dto = _mapper.Map<OpenSessionRequestDetailDto>(detail);
        await _pricingEnricher.EnrichDetailAsync(dto, detail, cancellationToken);
        return Success(entity: dto);
    }

    /// <summary>
    /// Published edit: accepted invitees stay; pending ones missing from the payload are removed;
    /// new (or previously declined) invitees become Pending, owned students are auto-accepted.
    /// </summary>
    private void MergePublishedInvitations(
        OpenSessionRequest entity,
        CreateOpenSessionRequestDto data,
        IReadOnlyCollection<int> ownedStudentIds,
        DateTime now)
    {
        var requested = data.InvitedStudentIds.Distinct().ToHashSet();

        var removed = entity.Invitations
            .Where(i => i.Status == OpenSessionRequestInvitationStatus.Pending
                        && !requested.Contains(i.InvitedStudentId))
            .ToList();
        foreach (var invite in removed)
        {
            entity.Invitations.Remove(invite);
            _db.Remove(invite);
        }

        foreach (var invitedId in requested)
        {
            var isOwned = ownedStudentIds.Contains(invitedId);
            var existing = entity.Invitations.FirstOrDefault(i => i.InvitedStudentId == invitedId);
            if (existing != null)
            {
                if (existing.Status is OpenSessionRequestInvitationStatus.Pending
                    or OpenSessionRequestInvitationStatus.Accepted)
                    continue;
                existing.Status = isOwned
                    ? OpenSessionRequestInvitationStatus.Accepted
                    : OpenSessionRequestInvitationStatus.Pending;
                existing.RespondedAt = isOwned ? now : null;
                continue;
            }

            entity.Invitations.Add(new OpenSessionRequestInvitation
            {
                InvitedStudentId = invitedId,
                InvitedByStudentId = data.StudentId,
                Status = isOwned
                    ? OpenSessionRequestInvitationStatus.Accepted
                    : OpenSessionRequestInvitationStatus.Pending,
                RespondedAt = isOwned ? now : null,
            });
        }
    }

    /// <summary>Auto-rejects pending offers and resets those teachers' targets; returns their teacher ids.</summary>
    private async Task<List<int>> AutoRejectPendingOffersAsync(
        int requestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var pendingOffers = await _db.OpenSessionOffers
            .Where(o => o.SessionRequestId == requestId
                        && o.Status == OpenSessionOfferStatus.Pending)
            .ToListAsync(cancellationToken);
        if (pendingOffers.Count == 0)
            return new List<int>();

        foreach (var offer in pendingOffers)
        {
            offer.Status = OpenSessionOfferStatus.AutoRejected;
            offer.RejectedAt = now;
            offer.RejectionReason = RequestEditedReason;
        }

        var teacherIds = pendingOffers.Select(o => o.TeacherId).Distinct().ToList();
        var targets = await _db.OpenSessionRequestTargets
            .Where(t => t.SessionRequestId == requestId
                        && teacherIds.Contains(t.TeacherId)
                        && t.Status == OpenSessionRequestTargetStatus.OfferSubmitted)
            .ToListAsync(cancellationToken);
        foreach (var target in targets)
        {
            target.Status = OpenSessionRequestTargetStatus.Notified;
            target.NotifiedAt = now;
        }

        return teacherIds;
    }

    /// <summary>Puts every existing target (incl. Skipped) back to Notified; returns their teacher ids.</summary>
    private async Task<List<int>> ResetTargetsForRepublishAsync(
        int requestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var targets = await _db.OpenSessionRequestTargets
            .Where(t => t.SessionRequestId == requestId)
            .ToListAsync(cancellationToken);
        foreach (var target in targets)
        {
            target.Status = OpenSessionRequestTargetStatus.Notified;
            target.NotifiedAt = now;
            target.ViewedAt = null;
        }
        return targets.Select(t => t.TeacherId).Distinct().ToList();
    }

    private async Task DispatchAfterRepublishAsync(
        OpenSessionRequest entity,
        List<int> existingTeacherIds,
        CancellationToken cancellationToken)
    {
        if (entity.Status != OpenSessionRequestStatus.Active)
            return;

        if (entity.TargetedTeacherId.HasValue)
        {
            var teacherId = entity.TargetedTeacherId.Value;
            if (!existingTeacherIds.Contains(teacherId))
            {
                await _targetingService.NotifyTargetedTeacherAsync(entity.Id, teacherId, cancellationToken);
                return;
            }
        }
        else
        {
            await _targetingService.RunMatchingAndNotifyAsync(entity.Id, cancellationToken);
        }

        await _targetingService.NotifyRequestRepublishedAsync(entity.Id, existingTeacherIds, cancellationToken);
    }

    private async Task DispatchAfterEditAsync(
        OpenSessionRequest entity,
        OpenSessionRequestStatus previousStatus,
        List<int> editedTeacherIds,
        CancellationToken cancellationToken)
    {
        if (entity.Status != OpenSessionRequestStatus.Active)
            return;

        var becameActive = previousStatus == OpenSessionRequestStatus.PendingInvitations;
        if (entity.TargetedTeacherId.HasValue)
        {
            var teacherId = entity.TargetedTeacherId.Value;
            if (becameActive)
            {
                await _targetingService.NotifyTargetedTeacherAsync(entity.Id, teacherId, cancellationToken);
                return;
            }
            if (!editedTeacherIds.Contains(teacherId))
                editedTeacherIds.Add(teacherId);
        }
        else
        {
            await _targetingService.RunMatchingAndNotifyAsync(entity.Id, cancellationToken);
        }

        await _targetingService.NotifyRequestEditedAsync(entity.Id, editedTeacherIds, cancellationToken);
    }
}
