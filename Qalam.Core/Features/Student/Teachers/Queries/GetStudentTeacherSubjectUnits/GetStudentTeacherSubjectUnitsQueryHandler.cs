using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherSubjectUnits;

public class GetStudentTeacherSubjectUnitsQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherSubjectUnitsQuery, Response<List<TeacherSubjectUnitOptionDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepertoireService _repertoireService;

    public GetStudentTeacherSubjectUnitsQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepertoireService repertoireService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _repertoireService = repertoireService;
    }

    public async Task<Response<List<TeacherSubjectUnitOptionDto>>> Handle(
        GetStudentTeacherSubjectUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher is null
            || teacher.Status != Qalam.Data.Entity.Common.Enums.TeacherStatus.Active
            || !teacher.IsActive)
        {
            return NotFound<List<TeacherSubjectUnitOptionDto>>("Teacher not found.");
        }

        var units = await _repertoireService.GetAllowedUnitsForTeacherSubjectAsync(
            request.TeacherId,
            request.TeacherSubjectId,
            cancellationToken);

        if (units is null)
        {
            return NotFound<List<TeacherSubjectUnitOptionDto>>("Teacher subject not found.");
        }

        return Success(entity: units);
    }
}
