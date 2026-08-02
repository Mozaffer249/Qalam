using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherSubjects;

public class GetStudentTeacherSubjectsQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherSubjectsQuery, Response<List<StudentTeacherSubjectDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;

    public GetStudentTeacherSubjectsQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
    }

    public async Task<Response<List<StudentTeacherSubjectDto>>> Handle(
        GetStudentTeacherSubjectsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher is null || teacher.Status != Qalam.Data.Entity.Common.Enums.TeacherStatus.Active || !teacher.IsActive)
            return NotFound<List<StudentTeacherSubjectDto>>("Teacher not found.");

        var limit = request.Limit is < 1 or > 50 ? 10 : request.Limit;

        var subjects = await _teacherSubjectRepository
            .GetApprovedActiveSubjectsWithUnitsAsync(request.TeacherId, cancellationToken);

        var dtos = subjects
            .Take(limit)
            .Select(ts => new StudentTeacherSubjectDto
            {
                SubjectId = ts.SubjectId,
                SubjectNameAr = ts.Subject?.NameAr ?? string.Empty,
                SubjectNameEn = ts.Subject?.NameEn ?? string.Empty,
                DomainId = ts.Subject?.DomainId,
                DomainCode = ts.Subject?.Domain?.Code,
                DomainNameAr = ts.Subject?.Domain?.NameAr,
                DomainNameEn = ts.Subject?.Domain?.NameEn,
                GradeNameAr = ts.Subject?.Grade?.NameAr,
                GradeNameEn = ts.Subject?.Grade?.NameEn,
                LevelNameAr = ts.Subject?.Level?.NameAr,
                LevelNameEn = ts.Subject?.Level?.NameEn,
                CurriculumNameAr = ts.Subject?.Curriculum?.NameAr,
                CurriculumNameEn = ts.Subject?.Curriculum?.NameEn,
                CanTeachFullSubject = ts.CanTeachFullSubject,
                UnitsCount = ts.TeacherSubjectUnits.Count,
                Units = ts.TeacherSubjectUnits.Select(u => new StudentTeacherSubjectUnitDto
                {
                    UnitId = u.UnitId,
                    UnitNameAr = u.Unit?.NameAr ?? string.Empty,
                    UnitNameEn = u.Unit?.NameEn ?? string.Empty,
                    UnitTypeCode = u.Unit?.UnitTypeCode,
                    QuranContentTypeId = u.QuranContentTypeId,
                    QuranContentTypeNameAr = u.QuranContentType?.NameAr,
                    QuranContentTypeNameEn = u.QuranContentType?.NameEn,
                    QuranLevelId = u.QuranLevelId,
                    QuranLevelNameAr = u.QuranLevel?.NameAr,
                    QuranLevelNameEn = u.QuranLevel?.NameEn,
                }).ToList()
            }).ToList();

        return Success(entity: dtos);
    }
}
