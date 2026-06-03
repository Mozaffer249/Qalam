using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSessionUnits;

public class UpdateCourseSessionUnitsCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCourseSessionUnitsCommand, Response<List<CourseSessionUnitDto>>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public UpdateCourseSessionUnitsCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<List<CourseSessionUnitDto>>> Handle(
        UpdateCourseSessionUnitsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.ReplaceSessionUnitsAsync(
                request.UserId,
                request.CourseId,
                request.SessionId,
                request.Data.Units,
                cancellationToken);

            if (result == null)
                return NotFound<List<CourseSessionUnitDto>>("Course or session not found.");

            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<List<CourseSessionUnitDto>>(ex.Message);
        }
    }
}
