using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetAcademicTermById;

public class GetAcademicTermByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAcademicTermByIdQuery, Response<AcademicTerm>>
{
    private readonly IGradeService _gradeService;

    public GetAcademicTermByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<AcademicTerm>> Handle(
        GetAcademicTermByIdQuery request,
        CancellationToken cancellationToken)
    {
        var term = await _gradeService.GetTermByIdAsync(request.Id);

        if (term == null)
            return NotFound<AcademicTerm>("Academic term not found");

        return Success(entity: term);
    }
}
