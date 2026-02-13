using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.DeleteCourse;

public class DeleteCourseCommandHandler : ResponseHandler,
    IRequestHandler<DeleteCourseCommand, Response<string>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public DeleteCourseCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<string>> Handle(
        DeleteCourseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (success, message) = await _teacherCourseService.DeleteCourseAsync(request.UserId, request.Id, cancellationToken);
            if (!success)
                return NotFound<string>(message);
            return Success<string>(Message: message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
