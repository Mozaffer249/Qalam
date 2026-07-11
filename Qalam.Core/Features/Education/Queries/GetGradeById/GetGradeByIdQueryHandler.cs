using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetGradeById;

public class GetGradeByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetGradeByIdQuery, Response<Grade>>
{
    private readonly IGradeService _gradeService;

    public GetGradeByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<Grade>> Handle(
        GetGradeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var grade = await _gradeService.GetGradeByIdAsync(request.Id);

        if (grade == null)
            return NotFound<Grade>("Grade not found");

        return Success(entity: grade);
    }
}
