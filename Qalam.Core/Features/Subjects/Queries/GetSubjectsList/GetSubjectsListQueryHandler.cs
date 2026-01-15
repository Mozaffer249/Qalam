using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Subjects.Queries.GetSubjectsList;

public class GetSubjectsListQueryHandler : ResponseHandler,
    IRequestHandler<GetSubjectsListQuery, Response<PaginatedResult<SubjectDto>>>
{
    private readonly ISubjectService _subjectService;

    public GetSubjectsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ISubjectService subjectService) : base(localizer)
    {
        _subjectService = subjectService;
    }

    public async Task<Response<PaginatedResult<SubjectDto>>> Handle(
        GetSubjectsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _subjectService.GetPaginatedSubjectsAsync(
            request.PageNumber,
            request.PageSize,
            // request.DomainId,
            // request.CurriculumId,
            // request.LevelId,
            request.GradeId,
            request.TermId,
            request.Search);

        return Success(entity: result);
    }
}
