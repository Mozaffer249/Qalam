using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnits;

public class GetTeacherSubjectUnitsQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherSubjectUnitsQuery, Response<List<TeacherSubjectUnitOptionDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepertoireService _repertoireService;

    public GetTeacherSubjectUnitsQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepertoireService repertoireService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _repertoireService = repertoireService;
    }

    public async Task<Response<List<TeacherSubjectUnitOptionDto>>> Handle(
        GetTeacherSubjectUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<List<TeacherSubjectUnitOptionDto>>("Teacher not found");
        }

        var units = await _repertoireService.GetAllowedUnitsForTeacherSubjectAsync(
            teacher.Id,
            request.TeacherSubjectId,
            cancellationToken);

        if (units == null)
        {
            return NotFound<List<TeacherSubjectUnitOptionDto>>("Teacher subject not found");
        }

        return Success(entity: units);
    }
}
