using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetSubjectByIdQuery, Response<Subject>>
{
    private readonly ISubjectService _subjectService;

    public GetSubjectByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ISubjectService subjectService) : base(localizer)
    {
        _subjectService = subjectService;
    }

    public async Task<Response<Subject>> Handle(
        GetSubjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectWithDetailsAsync(request.Id);
        
        if (subject == null)
            return NotFound<Subject>("Subject not found");

        return Success(entity: subject);
    }
}
