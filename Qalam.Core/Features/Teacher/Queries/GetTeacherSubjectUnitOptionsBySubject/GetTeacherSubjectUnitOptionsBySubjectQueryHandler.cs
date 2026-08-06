using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnitOptionsBySubject;

public class GetTeacherSubjectUnitOptionsBySubjectQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherSubjectUnitOptionsBySubjectQuery, Response<List<TeacherSubjectUnitPickerDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepertoireService _repertoireService;

    public GetTeacherSubjectUnitOptionsBySubjectQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepertoireService repertoireService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _repertoireService = repertoireService;
    }

    public async Task<Response<List<TeacherSubjectUnitPickerDto>>> Handle(
        GetTeacherSubjectUnitOptionsBySubjectQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherSubjectUnitPickerDto>>("Teacher not found");

        var units = await _repertoireService.GetUnitPickerBySubjectIdAsync(
            teacher.Id,
            request.SubjectId,
            cancellationToken);

        if (units == null)
            return NotFound<List<TeacherSubjectUnitPickerDto>>("Teacher subject not found");

        return Success(entity: units);
    }
}
