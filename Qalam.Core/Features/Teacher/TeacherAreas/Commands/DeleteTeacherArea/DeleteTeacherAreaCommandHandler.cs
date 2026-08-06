using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Commands.DeleteTeacherArea;

public class DeleteTeacherAreaCommandHandler : ResponseHandler,
    IRequestHandler<DeleteTeacherAreaCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherAreaRepository _teacherAreaRepository;

    public DeleteTeacherAreaCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherAreaRepository teacherAreaRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherAreaRepository = teacherAreaRepository;
    }

    public async Task<Response<string>> Handle(
        DeleteTeacherAreaCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher not found");

        var deleted = await _teacherAreaRepository.DeleteOwnedAsync(teacher.Id, request.Id, cancellationToken);
        if (!deleted)
            return NotFound<string>("Teacher area not found");

        return Deleted<string>("Teacher area deleted successfully");
    }
}
