using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherProfile;

public class GetStudentTeacherProfileQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherProfileQuery, Response<StudentTeacherProfileDto>>
{
    private readonly ITeacherRepository _teacherRepository;

    public GetStudentTeacherProfileQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<StudentTeacherProfileDto>> Handle(
        GetStudentTeacherProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _teacherRepository.GetStudentProfileAsync(request.TeacherId, cancellationToken);
        if (profile is null)
            return NotFound<StudentTeacherProfileDto>("Teacher not found.");

        return Success(entity: profile);
    }
}
