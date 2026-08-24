using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherSubjects;

public class GetStudentTeacherSubjectsQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherSubjectsQuery, Response<List<StudentTeacherSubjectDto>>>
{
    private const int MaxPageSize = 50;
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
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => request.PageSize
        };

        var result = await _teacherSubjectRepository.GetActiveSubjectsWithUnitsPagedAsync(
            request.TeacherId, pageNumber, pageSize, cancellationToken);

        if (result.TotalCount == 0)
        {
            var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
            if (teacher is null || teacher.Status != Qalam.Data.Entity.Common.Enums.TeacherStatus.Active || !teacher.IsActive)
                return NotFound<List<StudentTeacherSubjectDto>>("Teacher not found.");
        }

        var dtos = result.Items.Select(MapSubject).ToList();

        return Success(
            entity: dtos,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }

    private static StudentTeacherSubjectDto MapSubject(TeacherSubject ts) =>
        new()
        {
            TeacherSubjectId = ts.Id,
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
            }).ToList()
        };
}
