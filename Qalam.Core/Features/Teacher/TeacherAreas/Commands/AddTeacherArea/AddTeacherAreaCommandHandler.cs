using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.TeacherAreas;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Commands.AddTeacherArea;

public class AddTeacherAreaCommandHandler : ResponseHandler,
    IRequestHandler<AddTeacherAreaCommand, Response<TeacherAreaResponseDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherAreaRepository _teacherAreaRepository;

    public AddTeacherAreaCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherAreaRepository teacherAreaRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherAreaRepository = teacherAreaRepository;
    }

    public async Task<Response<TeacherAreaResponseDto>> Handle(
        AddTeacherAreaCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherAreaResponseDto>("Teacher not found");

        try
        {
            var area = await _teacherAreaRepository.AddAsync(
                teacher.Id,
                request.LocationId,
                request.MaxDistanceKm ?? 0m,
                cancellationToken);

            return Success("Teacher area added successfully", entity: TeacherAreaMapping.ToDto(area));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Location not found")
        {
            return NotFound<TeacherAreaResponseDto>("Location not found");
        }
    }
}
