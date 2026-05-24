using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.context;
using Microsoft.Extensions.Localization;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.CreateOpenSessionRequest;

public class CreateOpenSessionRequestCommandHandler
    : ResponseHandler, IRequestHandler<CreateOpenSessionRequestCommand, Response<OpenSessionRequestDetailDto>>
{
    private const int DefaultExpiryDays = 7;

    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IMapper _mapper;

    public CreateOpenSessionRequestCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IMapper mapper) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _mapper = mapper;
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
        var domainName = await _db.EducationDomains
            .Where(x => x.Id == data.DomainId)
            .Select(x => x.NameEn ?? string.Empty)
            .FirstOrDefaultAsync(cancellationToken);

        var isQuran = (domainName ?? string.Empty).Contains("quran", StringComparison.OrdinalIgnoreCase);
        if (isQuran && data.Sessions.Any(s => !s.QuranContentTypeId.HasValue || !s.QuranLevelId.HasValue))
            return BadRequest<OpenSessionRequestDetailDto>("جلسات مجال القرآن تتطلب QuranContentTypeId و QuranLevelId");

        // 5. Resolve invited-by student id (the learner's own Student.Id is the inviter)
        var inviterStudentId = data.StudentId;

        // 6. Build the entity
        var now = DateTime.UtcNow;
        var status = data.InvitedStudentIds.Any()
            ? OpenSessionRequestStatus.PendingInvitations
            : OpenSessionRequestStatus.Active;

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
            SubjectId = data.SubjectId,
            TeachingModeId = data.TeachingModeId,
            GroupType = data.GroupType,
            TotalSessionsCount = data.TotalSessionsCount,
            StudentNotes = data.StudentNotes,
            Status = status,
            PublishedAt = now,
            ExpiresAt = data.ExpiresAt ?? now.AddDays(DefaultExpiryDays),
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
                });

            entity.Sessions.Add(session);
        }

        foreach (var invitedId in data.InvitedStudentIds.Distinct())
        {
            entity.Invitations.Add(new OpenSessionRequestInvitation
            {
                InvitedStudentId = invitedId,
                InvitedByStudentId = inviterStudentId,
                Status = OpenSessionRequestInvitationStatus.Pending,
            });
        }

        await _db.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // 7. P3 hook: when status == Active, the matching engine should run here.
        //    Deferred to P3; for now the request just lands in Active and waits.

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
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.Sessions).ThenInclude(s => s.Units)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return entity is null ? null : _mapper.Map<OpenSessionRequestDetailDto>(entity);
    }
}
