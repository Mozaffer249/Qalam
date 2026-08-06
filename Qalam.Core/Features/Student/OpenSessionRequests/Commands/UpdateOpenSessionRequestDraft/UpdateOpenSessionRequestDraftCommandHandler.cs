using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.UpdateOpenSessionRequestDraft;

public class UpdateOpenSessionRequestDraftCommandHandler
    : ResponseHandler, IRequestHandler<UpdateOpenSessionRequestDraftCommand, Response<OpenSessionRequestDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly ITargetedOpenSessionRequestValidator _targetedValidator;
    private readonly IMapper _mapper;

    public UpdateOpenSessionRequestDraftCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        ITargetedOpenSessionRequestValidator targetedValidator,
        IMapper mapper) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _targetedValidator = targetedValidator;
        _mapper = mapper;
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

        if (entity.Status != OpenSessionRequestStatus.Draft)
            return BadRequest<OpenSessionRequestDetailDto>("يمكن تعديل المسودات فقط. استخدم Publish للنشر.");

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
        if (data.ExpiresAt.HasValue)
            entity.ExpiresAt = data.ExpiresAt;

        _db.RemoveRange(entity.Sessions.SelectMany(s => s.Units));
        _db.RemoveRange(entity.Sessions);
        _db.RemoveRange(entity.Invitations);
        entity.Sessions.Clear();
        entity.Invitations.Clear();

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
                    IncludesAllLessons = u.IncludesAllLessons,
                });
            entity.Sessions.Add(session);
        }

        foreach (var invitedId in data.InvitedStudentIds.Distinct())
        {
            entity.Invitations.Add(new OpenSessionRequestInvitation
            {
                InvitedStudentId = invitedId,
                InvitedByStudentId = data.StudentId,
                Status = OpenSessionRequestInvitationStatus.Pending,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

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
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .FirstAsync(r => r.Id == entity.Id, cancellationToken);

        return Success(entity: _mapper.Map<OpenSessionRequestDetailDto>(detail));
    }
}
