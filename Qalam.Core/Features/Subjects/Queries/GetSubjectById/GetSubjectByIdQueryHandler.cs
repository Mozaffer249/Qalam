using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetSubjectByIdQuery, Response<SubjectDto>>
{
    private readonly ISubjectService _subjectService;

    public GetSubjectByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ISubjectService subjectService) : base(localizer)
    {
        _subjectService = subjectService;
    }

    public async Task<Response<SubjectDto>> Handle(
        GetSubjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectDtoByIdAsync(request.Id);

        if (subject == null)
            return NotFound<SubjectDto>("Subject not found");

        return Success(entity: subject);
    }
}
