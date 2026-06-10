using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Queries.GetTeacherSubjectByIdForAdmin;

public class GetTeacherSubjectByIdForAdminQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherSubjectByIdForAdminQuery, Response<AdminTeacherSubjectDto?>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;

    public GetTeacherSubjectByIdForAdminQueryHandler(
        ITeacherSubjectAdminService subjectAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
    }

    public async Task<Response<AdminTeacherSubjectDto?>> Handle(
        GetTeacherSubjectByIdForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var subject = await _subjectAdminService.GetTeacherSubjectForAdminAsync(
            request.TeacherId, request.TeacherSubjectId, cancellationToken);

        if (subject == null)
            return NotFound<AdminTeacherSubjectDto?>("Teacher subject not found");

        return Success(entity: subject);
    }
}
