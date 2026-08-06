using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.TeacherAreas;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Queries.GetTeacherAreas;

public class GetTeacherAreasQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherAreasQuery, Response<List<TeacherAreaResponseDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherAreaRepository _teacherAreaRepository;

    public GetTeacherAreasQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherAreaRepository teacherAreaRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherAreaRepository = teacherAreaRepository;
    }

    public async Task<Response<List<TeacherAreaResponseDto>>> Handle(
        GetTeacherAreasQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherAreaResponseDto>>("Teacher not found");

        var areas = await _teacherAreaRepository.GetByTeacherIdWithLocationAsync(teacher.Id, cancellationToken);
        var dtos = areas.Select(TeacherAreaMapping.ToDto).ToList();
        return Success(entity: dtos);
    }
}
