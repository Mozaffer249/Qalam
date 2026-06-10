using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Queries.ListTeacherSubjects;

public class ListTeacherSubjectsQueryHandler : ResponseHandler,
    IRequestHandler<ListTeacherSubjectsQuery, Response<List<AdminTeacherSubjectDto>>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;

    public ListTeacherSubjectsQueryHandler(
        ITeacherSubjectAdminService subjectAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
    }

    public async Task<Response<List<AdminTeacherSubjectDto>>> Handle(
        ListTeacherSubjectsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _subjectAdminService.GetTeacherSubjectsListAsync(
            request.PageNumber,
            request.PageSize,
            request.TeacherId,
            request.SubjectId,
            request.IsActive,
            request.VerificationStatus,
            cancellationToken);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
