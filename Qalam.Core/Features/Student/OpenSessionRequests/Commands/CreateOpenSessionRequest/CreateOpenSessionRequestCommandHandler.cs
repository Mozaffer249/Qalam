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

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.CreateOpenSessionRequest;

public class CreateOpenSessionRequestCommandHandler
    : ResponseHandler, IRequestHandler<CreateOpenSessionRequestCommand, Response<OpenSessionRequestDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IOpenSessionRequestTargetingService _targetingService;
    private readonly ITargetedOpenSessionRequestValidator _targetedValidator;
    private readonly ITargetedOpenSessionRequestPricingService _targetedPricing;
    private readonly OpenSessionRequestSettings _osrSettings;
    private readonly IMapper _mapper;
    private readonly IGuardianChildrenService _guardianChildren;
    private readonly IOpenSessionRequestStudentPricingEnricher _pricingEnricher;

    public CreateOpenSessionRequestCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IOpenSessionRequestTargetingService targetingService,
        ITargetedOpenSessionRequestValidator targetedValidator,
        ITargetedOpenSessionRequestPricingService targetedPricing,
        IOptions<OpenSessionRequestSettings> osrSettings,
        IMapper mapper,
        IGuardianChildrenService guardianChildren,
        IOpenSessionRequestStudentPricingEnricher pricingEnricher) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _targetingService = targetingService;
        _targetedValidator = targetedValidator;
        _targetedPricing = targetedPricing;
        _osrSettings = osrSettings.Value;
        _mapper = mapper;
        _guardianChildren = guardianChildren;
        _pricingEnricher = pricingEnricher;
    }

    public async Task<Response<OpenSessionRequestDetailDto>> Handle(
        CreateOpenSessionRequestCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;

        // 1. Authorize: student (self, adult) or guardian (for a child)
        var access = await _accessGuard.CanCreateForStudentAsync(request.UserId, data.StudentId, cancellationToken);
        if (!access.Allowed)
            return Unauthorized<OpenSessionRequestDetailDto>(access.Reason ?? "Forbidden");

        // 2. Validate FK targets exist
        if (!await _db.EducationDomains.AnyAsync(x => x.Id == data.DomainId, cancellationToken))
            return NotFound<OpenSessionRequestDetailDto>("المجال غير موجود");
        if (!await _db.Subjects.AnyAsync(x => x.Id == data.SubjectId, cancellationToken))
            return NotFound<OpenSessionRequestDetailDto>("المادة غير موجودة");
        if (!await _db.TeachingModes.AnyAsync(x => x.Id == data.TeachingModeId, cancellationToken))
            return NotFound<OpenSessionRequestDetailDto>("طريقة التدريس غير موجودة");

        // 2b. Targeted-teacher branch — single service call covers: teacher existence + IsActive,
        // teacher offers the requested subject (active TeacherSubject), per-session unit rows
        // against TeacherSubjectUnits, and the row-level invariants (exactly-one-of, includesAllLessons + lessonId conflict).
        if (data.TargetedTeacherId.HasValue)
        {
            var err = await _targetedValidator.ValidateAsync(
                data.TargetedTeacherId.Value, data.SubjectId, data.Sessions, cancellationToken);
            if (err is not null)
                return BadRequest<OpenSessionRequestDetailDto>(err);
        }

        // 3. Validate invitations: invited students must exist & be active, no overlap with the learner
        if (data.InvitedStudentIds.Any())
        {
            var foundStudentIds = await _db.Students
                .Where(s => data.InvitedStudentIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            var missing = data.InvitedStudentIds.Except(foundStudentIds).ToList();
            if (missing.Any())
                return NotFound<OpenSessionRequestDetailDto>($"الطلاب المدعوون غير موجودون: {string.Join(", ", missing)}");
        }

        // 4. Quran-domain sessions require Quran content type + level
        var domain = await _db.EducationDomains
            .Where(x => x.Id == data.DomainId)
            .Select(x => new { x.Code, x.NameEn })
            .FirstOrDefaultAsync(cancellationToken);

        if (QuranDomainHelper.IsQuranDomain(domain?.Code, domain?.NameEn)
            && data.Sessions.Any(s => !s.QuranContentTypeId.HasValue || !s.QuranLevelId.HasValue))
            return BadRequest<OpenSessionRequestDetailDto>("جلسات مجال القرآن تتطلب QuranContentTypeId و QuranLevelId");

        // 5. Resolve invited-by student id (the learner's own Student.Id is the inviter)
        var inviterStudentId = data.StudentId;

        // Owned self + children: auto-Accepted (no pending invite). External → Pending.
        var ownedStudentIds = await _guardianChildren.GetOwnedStudentIdsAsync(
            request.UserId, cancellationToken);

        // 5b. First-session start + minimum lead (skipped for drafts — rechecked at publish)
        var now = DateTime.UtcNow;
        var isTargeted = data.TargetedTeacherId.HasValue;
        var firstSessionStartUtc = await OpenSessionRequestDeadlineResolver
            .ResolveFirstSessionStartUtcFromDtosAsync(_db, data.Sessions, cancellationToken);

        if (!data.AsDraft)
        {
            var leadError = OpenSessionRequestDeadlineResolver.ValidateMinimumLead(
                now, firstSessionStartUtc, _osrSettings, isTargeted);
            if (leadError != null)
                return BadRequest<OpenSessionRequestDetailDto>(leadError);
        }

        var invitedIds = data.InvitedStudentIds.Distinct().ToList();
        var hasExternalPending = invitedIds.Any(id => !ownedStudentIds.Contains(id));

        // 6. Build the entity
        OpenSessionRequestStatus status;
        DateTime? publishedAt;
        if (data.AsDraft)
        {
            status = OpenSessionRequestStatus.Draft;
            publishedAt = null;
        }
        else
        {
            status = hasExternalPending
                ? OpenSessionRequestStatus.PendingInvitations
                : OpenSessionRequestStatus.Active;
            publishedAt = now;
        }

        var expiresAt = OpenSessionRequestDeadlineResolver.ResolveExpiry(
            now, data.ExpiresAt, firstSessionStartUtc, _osrSettings, isTargeted);

        var entity = new OpenSessionRequest
        {
            StudentId = data.StudentId,
            RequestedByUserId = request.UserId,
            CreatedByGuardianId = access.GuardianId,
            DomainId = data.DomainId,
            CurriculumId = data.CurriculumId,
            LevelId = data.LevelId,
            GradeId = data.GradeId,
            TermId = data.TermId,
            UniversityId = data.UniversityId,
            CollegeId = data.CollegeId,
            DepartmentId = data.DepartmentId,
            AcademicProgramId = data.AcademicProgramId,
            SubjectId = data.SubjectId,
            TeachingModeId = data.TeachingModeId,
            TargetedTeacherId = data.TargetedTeacherId,
            GroupType = data.GroupType,
            TotalSessionsCount = data.TotalSessionsCount,
            StudentNotes = data.StudentNotes,
            Status = status,
            PublishedAt = publishedAt,
            ExpiresAt = expiresAt,
        };

        foreach (var s in data.Sessions)
        {
            var session = new OpenSessionRequestSession
            {
                SequenceNumber = s.SequenceNumber,
                PreferredDate = s.PreferredDate,
                TimeSlotId = s.TimeSlotId,
                DurationMinutes = s.DurationMinutes,
                QuranContentTypeId = s.QuranContentTypeId,
                QuranLevelId = s.QuranLevelId,
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

        foreach (var invitedId in invitedIds)
        {
            var isOwned = ownedStudentIds.Contains(invitedId);
            entity.Invitations.Add(new OpenSessionRequestInvitation
            {
                InvitedStudentId = invitedId,
                InvitedByStudentId = inviterStudentId,
                Status = isOwned
                    ? OpenSessionRequestInvitationStatus.Accepted
                    : OpenSessionRequestInvitationStatus.Pending,
                RespondedAt = isOwned ? now : null,
            });
        }

        await _db.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Freeze directed price as soon as the request is published (not drafts — sessions may still change).
        if (!data.AsDraft && entity.TargetedTeacherId.HasValue)
        {
            await _targetedPricing.FreezeIfNeededAsync(entity, request.UserId, cancellationToken);
        }

        // 7. P3: dispatch to the chosen teacher (targeted) or run broadcast matching (default)
        //    when the request is publishable now (no pending invitations).
        //    If status is PendingInvitations, dispatch waits for invitations to resolve — see the
        //    invitation handler for that path.
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

        // 8. Reload with all navigations for the response DTO
        var detail = await BuildDetailAsync(entity.Id, cancellationToken);
        return Success(entity: detail!);
    }

    private async Task<OpenSessionRequestDetailDto?> BuildDetailAsync(int id, CancellationToken ct)
    {
        var entity = await _db.OpenSessionRequests
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
            .Include(r => r.TargetedTeacher).ThenInclude(t => t!.User)
            .Include(r => r.Sessions).ThenInclude(s => s.QuranContentType)
            .Include(r => r.Sessions).ThenInclude(s => s.QuranLevel)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .Include(r => r.Offers)
            .Include(r => r.PricingSnapshot)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (entity is null)
            return null;

        var dto = _mapper.Map<OpenSessionRequestDetailDto>(entity);
        await _pricingEnricher.EnrichDetailAsync(dto, entity, ct);
        return dto;
    }
}
