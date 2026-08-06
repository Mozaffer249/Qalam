using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.DeleteTeacherSubject;

public class DeleteTeacherSubjectCommandHandler : ResponseHandler,
    IRequestHandler<DeleteTeacherSubjectCommand, Response<string>>
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherRepository _teacherRepository;

    public DeleteTeacherSubjectCommandHandler(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<string>> Handle(
        DeleteTeacherSubjectCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher not found");

        var teacherSubject = await _teacherSubjectRepository.GetByIdForTeacherAsync(
            teacher.Id,
            request.Id,
            cancellationToken);

        if (teacherSubject == null)
            return NotFound<string>("Teacher subject not found");

        await _teacherSubjectRepository.DeleteOwnedAsync(teacher.Id, request.Id, cancellationToken);

        return Deleted<string>("Teacher subject deleted successfully");
    }
}
