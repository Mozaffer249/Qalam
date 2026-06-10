using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Queries.GetTeacherSubjectsForAdmin;

public class GetTeacherSubjectsForAdminQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherSubjectsForAdminQuery, Response<List<AdminTeacherSubjectDto>>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;
    private readonly ITeacherRepository _teacherRepository;

    public GetTeacherSubjectsForAdminQueryHandler(
        ITeacherSubjectAdminService subjectAdminService,
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<List<AdminTeacherSubjectDto>>> Handle(
        GetTeacherSubjectsForAdminQuery request,
        CancellationToken cancellationToken)
    {
        if (await _teacherRepository.GetByIdAsync(request.TeacherId) == null)
            return NotFound<List<AdminTeacherSubjectDto>>("Teacher not found");

        var subjects = await _subjectAdminService.GetTeacherSubjectsForAdminAsync(request.TeacherId, cancellationToken);
        return Success(entity: subjects);
    }
}
